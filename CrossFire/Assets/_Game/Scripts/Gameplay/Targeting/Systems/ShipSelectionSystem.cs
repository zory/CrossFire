using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	//[BurstCompile]
	public partial struct ShipSelectionSystem : ISystem
	{
		private EntityQuery _requestQuery;

		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<SelectionRequestBufferTag>(),
				ComponentType.ReadOnly<SelectionRequestCommand>() // buffer type
			);

			state.RequireForUpdate(_requestQuery);
		}

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<SelectionRequestCommand> commandBuffer = entityManager.GetBuffer<SelectionRequestCommand>(commandEntity);

			if (commandBuffer.IsEmpty)
			{
				return;
			}

			NativeArray<SelectionRequestCommand> commands = commandBuffer.ToNativeArray(Allocator.Temp);
			commandBuffer.Clear();

			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach (Entity entity in
					 SystemAPI.QueryBuilder().
						WithAll<ControlledTag>().
						Build().
						ToEntityArray(Allocator.Temp))
			{
				entityCommandBuffer.RemoveComponent<ControlledTag>(entity);
			}

			for (int index = 0; index < commands.Length; index++)
			{
				SelectionRequestCommand command = commands[index];
				Entity selectedEntity = GetClosestSelectableEntity(ref state, command.WorldPosition, command.PickRadius);

				if (selectedEntity != Entity.Null)
				{
					entityCommandBuffer.AddComponent<ControlledTag>(selectedEntity);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
			commands.Dispose();
		}

		private Entity GetClosestSelectableEntity(ref SystemState state, float2 worldPosition, float pickRadius)
		{
			Entity bestEntity = Entity.Null;
			float bestDistanceSq = pickRadius * pickRadius;

			foreach ((RefRO<WorldPose> worldPose, Entity entity) in 
					 SystemAPI.Query<RefRO<WorldPose>>().
						WithAll<SelectableTag>().
						WithEntityAccess())
			{
				float2 delta = worldPose.ValueRO.Value.Position - worldPosition;
				float distanceSq = math.dot(delta, delta);

				if (distanceSq <= bestDistanceSq)
				{
					bestDistanceSq = distanceSq;
					bestEntity = entity;
				}
			}

			return bestEntity;
		}
	}
}
