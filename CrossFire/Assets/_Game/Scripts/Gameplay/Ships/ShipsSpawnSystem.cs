using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using static PlasticPipe.PlasticProtocol.Messages.NegotiationCommand;

namespace CrossFire.Ships
{
	public enum ShipType : int
	{
		Fighter = 0,
		Bomber = 1,
		Carrier = 2,

		Sample1 = 3,
		Sample2 = 4,
		Sample3 = 5,
	}

	public struct SpawnShipsCommandBufferTag : IComponentData
	{
	}

	public struct SpawnShipsCommand : IBufferElementData
	{
		public int Id;
		public ShipType Type;
		public byte Team;
		public Pose2D Pose;

		public override string ToString()
		{
			return
				string.Format(
					"SpawnShipsCommand. " +
					"Id:{0} " +
					"Type:{1} " +
					"Team:{2} " +
					"Pose:{3}",
					Id, Type, Team, Pose
				);
		}
	}

	[DisableAutoCreation]
	public partial struct ShipsSpawnCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SpawnShipsCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<SpawnShipsCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}

	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateBefore(typeof(SnapshotSystem))]
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ShipsSpawnSystem : ISystem
	{
		private EntityQuery _requestQuery;
 
		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(
						ComponentType.ReadOnly<SpawnShipsCommandBufferTag>(),
						ComponentType.ReadOnly<SpawnShipsCommand>() // buffer type
					);

			state.RequireForUpdate(_requestQuery);
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(commandEntity);

			if (commandBuffer.IsEmpty)
			{
				return;
			}

			NativeArray<SpawnShipsCommand> commands = commandBuffer.ToNativeArray(Allocator.Temp);
			commandBuffer.Clear();

			for (int index = 0; index < commands.Length; index++)
			{
				SpawnShipsCommand command = commands[index];

				Entity prefabEntity = GetPrefabForType(ref state, command.Type);
				if (prefabEntity == Entity.Null)
				{
					continue;
				}

				Entity shipEntity = entityManager.Instantiate(prefabEntity);

				byte teamId = command.Team;
				SetId(entityManager, shipEntity, command.Id);
				SetTeam(entityManager, shipEntity, teamId);

				float4 teamColor = CoreHelpers.GetTeamColor(entityManager, teamId);
				SetNativeColor(entityManager, shipEntity, teamColor);
				entityManager.AddComponentData<NeedsColorRefresh>(shipEntity,
					new NeedsColorRefresh()
					{
						Value = CoreHelpers.GetTeamColor(entityManager, teamId)
					}
				);

				SetPose(entityManager, shipEntity, command.Pose);

				if (!entityManager.HasComponent<NeedsTargetTag>(shipEntity))
				{
					entityManager.AddComponent<NeedsTargetTag>(shipEntity);
				}
			}

			commands.Dispose();
		}

		private Entity GetPrefabForType(ref SystemState state, ShipType shipType)
		{
			DynamicBuffer<ShipPrefabEntry> entries = SystemAPI.GetSingletonBuffer<ShipPrefabEntry>(true);

			for (int index = 0; index < entries.Length; index++)
			{
				ShipPrefabEntry entry = entries[index];
				if (entry.Type == shipType)
					return entry.Prefab;
			}

			return Entity.Null;
		}

		private static void SetId(EntityManager entityManager, Entity entity, int id)
		{
			entityManager.SetComponentData(entity, new StableId { Value = id });
		}

		private static void SetTeam(EntityManager entityManager, Entity entity, byte teamId)
		{
			entityManager.SetComponentData(entity, new TeamId { Value = teamId });
		}

		public static void SetNativeColor(EntityManager entityManager, Entity entity, float4 color)
		{
			entityManager.SetComponentData(entity, new NativeColor { Value = color });
		}

		private static void SetPose(EntityManager entityManager, Entity entity, Pose2D pose)
		{
			float3 position = new float3(pose.Position.x, pose.Position.y, 0f);
			quaternion rotation = quaternion.RotateZ(pose.ThetaRad);
			entityManager.SetComponentData(entity, new PrevWorldPose() { Value = pose });
			entityManager.SetComponentData(entity, new WorldPose() { Value = pose });
			entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1));
		}
	}
}