using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct ShipSpawnSystem : ISystem
{
	private bool _spawned;

	public void OnCreate(ref SystemState state)
	{
		_spawned = false;

		state.RequireForUpdate<BattleConfig>();
		state.RequireForUpdate<ShipPrefabRef>();
		state.RequireForUpdate<TeamConfigElement>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		if (_spawned)
		{
			return;
		}

		var entityManager = state.EntityManager;
		BattleConfig battleConfig = SystemAPI.GetSingleton<BattleConfig>();
		DynamicBuffer<TeamConfigElement> teamBufRO = SystemAPI.GetSingletonBuffer<TeamConfigElement>(true);

		int teamCount = teamBufRO.Length;
		var teamsData = new List<TeamConfigElement>(teamCount);
		for (int i = 0; i < teamCount; i++)
		{
			teamsData[i] = new TeamConfigElement()
			{
				ColorRGBA = teamBufRO[i].ColorRGBA,
				TotalShips = teamBufRO[i].TotalShips
			};
		}

		Entity shipPrefab = SystemAPI.GetSingleton<ShipPrefabRef>().Value;
		float2 shipPrefabSize = float2.zero;
		if (entityManager.HasComponent<Size>(shipPrefab))
		{
			shipPrefabSize = entityManager.GetComponentData<Size>(shipPrefab).Value;
		}

		// Read spawn areas (each TeamSpawnAreaAuthoring bakes its own SpawnAreaElement buffer)
		List<SpawnAreaElement>[] areasByTeam = new List<SpawnAreaElement>[teamCount];
		for (int teamIdx = 0; teamIdx < teamCount; teamIdx++)
		{
			areasByTeam[teamIdx] = new List<SpawnAreaElement>();
		}
		using (EntityQuery qAreas = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnAreaElement>()))
		{
			using (NativeArray<Entity> areaEntities = qAreas.ToEntityArray(Unity.Collections.Allocator.Temp))
			{
				for (int i = 0; i < areaEntities.Length; i++)
				{
					var buffer = entityManager.GetBuffer<SpawnAreaElement>(areaEntities[i], true);
					for (int j = 0; j < buffer.Length; j++)
					{
						var a = buffer[j];
						if (a.Team < teamCount)
						{
							areasByTeam[a.Team].Add(a);
						}
					}
				}
			}
		}

		var rng = new Unity.Mathematics.Random((uint)math.max(1, battleConfig.SpawnSeed));

		CollisionRadius collisionRadius = default;
		if (entityManager.HasComponent<CollisionRadius>(shipPrefab))
		{
			collisionRadius = entityManager.GetComponentData<CollisionRadius>(shipPrefab);
		}
		float minDist = collisionRadius.Value * 2f;
		float minDistSq = minDist * minDist;
		float spawnCellSize = minDist;

		Entity controlledEntity = Entity.Null;

		for (int teamIdx = 0; teamIdx < teamCount; teamIdx++)
		{
			int shipCount = teamsData[teamIdx].TotalShips;

			// team-local no-overlap grid (cell -> positions)
			Dictionary<long, List<float2>> grid = new Dictionary<long, List<float2>>(shipCount * 2);

			for (int shipIdx = 0; shipIdx < shipCount; shipIdx++)
			{
				float2 position = float2.zero;
				for (int attempt = 0; attempt < battleConfig.MaxSpawnAttemptsPerShip; attempt++)
				{
					position = ShipSystemHelper.SamplePoint(areasByTeam[teamIdx], ref rng);
					if (ShipSystemHelper.IsFree(position, minDistSq, spawnCellSize, grid))
					{
						break;
					}
				}
				ShipSystemHelper.AddToGrid(position, spawnCellSize, grid);

				Entity entity = entityManager.Instantiate(shipPrefab);

				// Core state
				entityManager.SetComponentData(entity, new TeamId { Value = (byte)teamIdx });
				entityManager.SetComponentData(entity, new Pos { Value = position });
				entityManager.SetComponentData(entity, new PrevPos { Value = position });
				float theta = rng.NextFloat(0f, math.PI * 2f);
				entityManager.SetComponentData(entity, new Angle { Value = theta });
				entityManager.SetComponentData(entity, new Velocity { Value = float2.zero });

				// Transform for rendering
				entityManager.SetComponentData(
					entity,
					LocalTransform.FromPositionRotationScale(
						new float3(position.x, position.y, 0f),
						quaternion.RotateZ(theta),
						1f
					)
				);

				// Non-uniform ship size
				if (!entityManager.HasComponent<PostTransformMatrix>(entity))
				{
					entityManager.AddComponentData(entity, new PostTransformMatrix { Value = float4x4.identity });
				}
				entityManager.SetComponentData(entity, new PostTransformMatrix
				{
					Value = float4x4.Scale(new float3(shipPrefabSize.x, shipPrefabSize.y, 1f))
				});

				// Team color (Entities Graphics / URP)
				float4 color = teamsData[teamIdx].ColorRGBA;
				if (!entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
				{
					entityManager.AddComponentData(entity, new URPMaterialPropertyBaseColor { Value = color });
				}
				else
				{
					entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor { Value = color });
				}

				// Choose controlled ship: first spawned ship of team 0 (fallback: first spawned overall)
				if (controlledEntity == Entity.Null)
				{
					controlledEntity = entity;
				}
			}
		}

		// Ensure ControlledShip singleton exists
		if (!SystemAPI.HasSingleton<ControlledShip>())
		{
			Entity controlledShip = entityManager.CreateEntity(typeof(ControlledShip));
			entityManager.SetComponentData(controlledShip, new ControlledShip { Value = controlledEntity });
		}

		_spawned = true;
	}
}