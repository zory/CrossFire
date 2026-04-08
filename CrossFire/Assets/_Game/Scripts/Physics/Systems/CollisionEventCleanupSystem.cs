using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Core.Physics
{
	/// <summary>
	/// prevent events from persisting next frame
	/// </summary>
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(PostPhysicsSystem))]
	//[UpdateBefore(typeof(TransformSystemGroup))]
	[DisableAutoCreation]
	public partial struct CollisionEventCleanupSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			Entity collisionEventBufferEntity = SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();
			DynamicBuffer<CollisionEvent> collisionEventBuffer = state.EntityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);
			collisionEventBuffer.Clear();
		}
	}
}