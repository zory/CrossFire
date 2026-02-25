using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace CrossFire.Ships
{
	public struct SpawnShipsCommandBufferTag : IComponentData
	{
	}

	public struct SpawnShipsCommand : IBufferElementData
	{
		public int Id;
		public byte Team;
		public float4 ColorRGBA;
		public Pose2D Pose;

		public override string ToString()
		{
			return
				string.Format(
					"SpawnShipsCommand. " +
					"Id:{0} " +
					"Team:{1} " +
					"Color:{2} " +
					"Pose:{3}",
					Id, Team, ColorRGBA, Pose
				);
		}
	}

	public partial struct ShipsSpawnCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity commandBufferEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SpawnShipsCommandBufferTag>(commandBufferEntity);
			state.EntityManager.AddBuffer<SpawnShipsCommand>(commandBufferEntity);
		}

		public void OnUpdate(ref SystemState state) { }
	}

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateBefore(typeof(TransformSystemGroup))]
	[BurstCompile]
	public partial struct ShipsSpawnSystem : ISystem
	{
		private EntityQuery _requestQuery;

		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(SpawnShipsCommandBufferTag),
					typeof(SpawnShipsCommand)
				}
			});

			state.RequireForUpdate(_requestQuery);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(commandEntity);
			if (commandBuffer.IsEmpty)
				return;
			NativeArray<SpawnShipsCommand> commands = commandBuffer.ToNativeArray(Allocator.Temp);
			commandBuffer.Clear();

			ShipPrefabReference prefabReference = SystemAPI.GetSingleton<ShipPrefabReference>();

			for (int index = 0; index < commands.Length; index++)
			{
				SpawnShipsCommand command = commands[index];

				Entity shipEntity = entityManager.Instantiate(prefabReference.Prefab);

				SetId(entityManager, shipEntity, command.Id);
				SetTeam(entityManager, shipEntity, command.Team);
				SetNativeColor(entityManager, shipEntity, command.ColorRGBA);
				SetColor(entityManager, shipEntity, command.ColorRGBA);
				SetPose(entityManager, shipEntity, command.Pose);
			}

			commands.Dispose();
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

		public static void SetColor(EntityManager entityManager, Entity entity, float4 color)
		{
			entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor { Value = color });
		}

		private static void SetPose(EntityManager entityManager, Entity entity, Pose2D pose)
		{
			float3 position = new float3(pose.Position.x, pose.Position.y, 0f);
			quaternion rotation = quaternion.RotateZ(pose.Theta);
			entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1));
		}
	}
}