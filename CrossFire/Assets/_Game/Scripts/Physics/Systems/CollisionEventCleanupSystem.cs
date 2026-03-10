using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct CollisionEventCleanupSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			Entity collisionEventBufferEntity =
				SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();

			DynamicBuffer<CollisionEvent> collisionEventBuffer =
				state.EntityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);

			collisionEventBuffer.Clear();
		}
	}
}