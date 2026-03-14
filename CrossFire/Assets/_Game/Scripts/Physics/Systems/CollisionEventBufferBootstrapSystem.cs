using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	/// <summary>
	/// Initializes collision event buffer entity
	/// </summary>
	[BurstCompile]
	//[UpdateInGroup(typeof(InitializationSystemGroup))]
	[DisableAutoCreation]
	public partial struct CollisionEventBufferBootstrapSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			EntityQuery collisionEventBufferQuery = state.GetEntityQuery(ComponentType.ReadOnly<CollisionEventBufferTag>());

			if (!collisionEventBufferQuery.IsEmptyIgnoreFilter)
			{
				return;
			}

			Entity eventBufferEntity = state.EntityManager.CreateEntity();

			//Add entity with CollisionEventTag and Place CollisionEventBuffer on it
			state.EntityManager.AddComponent<CollisionEventBufferTag>(eventBufferEntity);
			state.EntityManager.AddBuffer<CollisionEvent>(eventBufferEntity);
		}

		public void OnUpdate(ref SystemState state)
		{
		}
	}
}