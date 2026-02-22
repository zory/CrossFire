using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ShipSpawnSystem : ISystem
{
	bool spawned;

	public void OnCreate(ref SystemState state)
	{
		spawned = false;
		state.RequireForUpdate<BattleConfig>();
		state.RequireForUpdate<ShipPrefabRef>();
		state.RequireForUpdate<TeamConfigElement>();
		state.RequireForUpdate<PlayerInput>(); // ensures bridge created singleton
	}

	public void OnUpdate(ref SystemState state)
	{
		if (spawned) return;

		var em = state.EntityManager;
		var cfg = SystemAPI.GetSingleton<BattleConfig>();
		var shipPrefab = SystemAPI.GetSingleton<ShipPrefabRef>().Value;
		var teamBufRO = SystemAPI.GetSingletonBuffer<TeamConfigElement>(true);

		// Copy colors NOW (before any Instantiate/AddComponent calls)
		float4[] teamColors = new float4[teamBufRO.Length];
		for (int i = 0; i < teamBufRO.Length; i++)
			teamColors[i] = teamBufRO[i].ColorRGBA;

		// Ensure ControlledShip singleton exists
		if (!SystemAPI.HasSingleton<ControlledShip>())
		{
			var s = em.CreateEntity(typeof(ControlledShip));
			em.SetComponentData(s, new ControlledShip { Value = Entity.Null });
		}

		// Read spawn areas (each TeamSpawnAreaAuthoring bakes its own SpawnAreaElement buffer)
		var areasByTeam = new List<SpawnAreaElement>[cfg.TeamCount];
		for (int t = 0; t < cfg.TeamCount; t++) areasByTeam[t] = new List<SpawnAreaElement>();

		using (var qAreas = em.CreateEntityQuery(ComponentType.ReadOnly<SpawnAreaElement>()))
		using (var areaEntities = qAreas.ToEntityArray(Unity.Collections.Allocator.Temp))
		{
			for (int i = 0; i < areaEntities.Length; i++)
			{
				var buf = em.GetBuffer<SpawnAreaElement>(areaEntities[i], true);
				for (int k = 0; k < buf.Length; k++)
				{
					var a = buf[k];
					if (a.Team < cfg.TeamCount)
						areasByTeam[a.Team].Add(a);
				}
			}
		}

		int baseCount = cfg.TotalShips / cfg.TeamCount;
		int remainder = cfg.TotalShips % cfg.TeamCount;

		var rng = new Unity.Mathematics.Random((uint)math.max(1, cfg.SpawnSeed));

		float minDist = cfg.ShipRadius * 2f;
		float minDistSq = minDist * minDist;
		float spawnCellSize = minDist;

		Entity chosenControlled = Entity.Null;

		for (int team = 0; team < cfg.TeamCount; team++)
		{
			int count = baseCount + (team < remainder ? 1 : 0);

			// team-local no-overlap grid (cell -> positions)
			var grid = new Dictionary<long, List<float2>>(count * 2);

			for (int n = 0; n < count; n++)
			{
				float2 p = 0;
				bool placed = false;

				for (int attempt = 0; attempt < cfg.MaxSpawnAttemptsPerShip; attempt++)
				{
					p = SamplePoint(team, areasByTeam, ref rng);
					if (IsFree(p, minDistSq, spawnCellSize, grid))
					{
						placed = true;
						break;
					}
				}

				// If too dense: accept last p to guarantee progress
				if (!placed)
				{
					// optional warning
					// Debug.LogWarning($"Team {team}: spawn too dense; accepting overlap.");
				}

				AddToGrid(p, spawnCellSize, grid);

				var e = em.Instantiate(shipPrefab);

				// Choose controlled ship: first spawned ship of team 0 (fallback: first spawned overall)
				if (chosenControlled == Entity.Null && (team == 0 || cfg.TeamCount == 1))
					chosenControlled = e;
				if (chosenControlled == Entity.Null)
					chosenControlled = e;

				// Core state
				em.SetComponentData(e, new TeamId { Value = (byte)team });
				em.SetComponentData(e, new ShipPos { Value = p });
				em.SetComponentData(e, new ShipPrevPos { Value = p });

				float theta = rng.NextFloat(0f, math.PI * 2f);
				em.SetComponentData(e, new ShipAngle { Value = theta });

				em.SetComponentData(e, new ShipRadius { Value = cfg.ShipRadius });
				em.SetComponentData(e, new ShipSpeed { Value = cfg.ShipSpeed });
				em.SetComponentData(e, new ShipTurnSpeed { Value = cfg.ShipTurnSpeedRad });

				// Transform for rendering
				em.SetComponentData(e, LocalTransform.FromPositionRotationScale(
					new float3(p.x, p.y, 0f),
					quaternion.RotateZ(theta),
					1f));

				// Non-uniform ship size
				if (!em.HasComponent<PostTransformMatrix>(e))
					em.AddComponentData(e, new PostTransformMatrix { Value = float4x4.identity });

				em.SetComponentData(e, new PostTransformMatrix
				{
					Value = float4x4.Scale(new float3(cfg.ShipSizeXY.x, cfg.ShipSizeXY.y, 1f))
				});

				// Team color (Entities Graphics / URP)
				var c = teamColors[team];
				if (!em.HasComponent<URPMaterialPropertyBaseColor>(e))
					em.AddComponentData(e, new URPMaterialPropertyBaseColor { Value = c });
				else
					em.SetComponentData(e, new URPMaterialPropertyBaseColor { Value = c });

				if (!em.HasComponent<ShipHp>(e))
					em.AddComponentData(e, new ShipHp { Value = 3 }); // set default HP
				else
					em.SetComponentData(e, new ShipHp { Value = 3 });
			}
		}

		// Set controlled ship singleton
		var selEntity = SystemAPI.GetSingletonEntity<ControlledShip>();
		em.SetComponentData(selEntity, new ControlledShip { Value = chosenControlled });

		spawned = true;
	}

	static float2 SamplePoint(int team, List<SpawnAreaElement>[] areasByTeam, ref Unity.Mathematics.Random rng)
	{
		var list = areasByTeam[team];
		if (list == null || list.Count == 0)
		{
			// fallback: random circle around origin
			float2 dir = rng.NextFloat2Direction();
			float r = rng.NextFloat(0f, 40f);
			return dir * r;
		}

		int idx = rng.NextInt(0, list.Count);
		var a = list[idx];

		float x = rng.NextFloat(a.Min.x, a.Max.x);
		float y = rng.NextFloat(a.Min.y, a.Max.y);
		return new float2(x, y);
	}

	static bool IsFree(float2 p, float minDistSq, float cellSize, Dictionary<long, List<float2>> grid)
	{
		int2 c = (int2)math.floor(p / cellSize);

		for (int oy = -1; oy <= 1; oy++)
			for (int ox = -1; ox <= 1; ox++)
			{
				int2 nc = c + new int2(ox, oy);
				long key = HashCell64(nc);

				if (!grid.TryGetValue(key, out var list)) continue;

				for (int i = 0; i < list.Count; i++)
				{
					float2 d = p - list[i];
					if (math.lengthsq(d) < minDistSq) return false;
				}
			}

		return true;
	}

	static void AddToGrid(float2 p, float cellSize, Dictionary<long, List<float2>> grid)
	{
		int2 c = (int2)math.floor(p / cellSize);
		long key = HashCell64(c);

		if (!grid.TryGetValue(key, out var list))
		{
			list = new List<float2>(4);
			grid.Add(key, list);
		}
		list.Add(p);
	}

	static long HashCell64(int2 c)
	{
		unchecked { return ((long)c.x << 32) ^ (uint)c.y; }
	}
}