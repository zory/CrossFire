using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrossFire.Ships
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ShipsSpawnSystem : ISystem
	{
		private EntityQuery _requestQuery;

		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<SpawnShipsCommandBufferTag>(),
				ComponentType.ReadOnly<SpawnShipsCommand>()
			);

			state.RequireForUpdate(_requestQuery);
			state.RequireForUpdate<ShipPrefabEntry>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(commandEntity);

			if (commandBuffer.IsEmpty)
			{
				return;
			}

			DynamicBuffer<ShipPrefabEntry> prefabEntries =
				SystemAPI.GetSingletonBuffer<ShipPrefabEntry>(true);

			DynamicBuffer<TeamColor> teamColors = default;
			bool hasTeamColors = SystemAPI.HasSingleton<TeamColor>();
			if (hasTeamColors)
			{
				teamColors = SystemAPI.GetSingletonBuffer<TeamColor>(true);
			}

			NativeArray<SpawnShipsCommand> commands = commandBuffer.ToNativeArray(Allocator.Temp);
			commandBuffer.Clear();

			EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

			for (int index = 0; index < commands.Length; index++)
			{
				SpawnShipsCommand command = commands[index];

				Entity prefabEntity = GetPrefabForType(prefabEntries, command.Type);
				if (prefabEntity == Entity.Null)
				{
					continue;
				}

				Entity shipEntity = ecb.Instantiate(prefabEntity);

				byte teamId = command.Team;
				float4 teamColor = hasTeamColors
					? CoreHelpers.GetTeamColor(teamColors, teamId)
					: new float4(1f, 1f, 1f, 1f);

				ecb.SetComponent(shipEntity, new StableId
				{
					Value = command.Id
				});

				ecb.SetComponent(shipEntity, new TeamId
				{
					Value = teamId
				});

				ecb.SetComponent(shipEntity, new NativeColor
				{
					Value = teamColor
				});

				ecb.AddComponent(shipEntity, new NeedsColorRefresh
				{
					Value = teamColor
				});

				SetPose(ecb, shipEntity, command.Pose);
			}

			ecb.Playback(entityManager);
			ecb.Dispose();
			commands.Dispose();
		}

		private static Entity GetPrefabForType(
			DynamicBuffer<ShipPrefabEntry> entries,
			ShipType shipType)
		{
			for (int index = 0; index < entries.Length; index++)
			{
				ShipPrefabEntry entry = entries[index];
				if (entry.Type == shipType)
				{
					return entry.Prefab;
				}
			}

			return Entity.Null;
		}

		private static void SetPose(EntityCommandBuffer ecb, Entity entity, Pose2D pose)
		{
			float3 position = new float3(pose.Position.x, pose.Position.y, 0f);
			quaternion rotation = quaternion.RotateZ(pose.ThetaRad);

			ecb.SetComponent(entity, new PrevWorldPose
			{
				Value = pose
			});

			ecb.SetComponent(entity, new WorldPose
			{
				Value = pose
			});

			ecb.SetComponent(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1f));
		}
	}
}