using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct CollisionEventBufferBootstrapSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			EntityQuery collisionEventBufferQuery =
				state.GetEntityQuery(ComponentType.ReadOnly<CollisionEventBufferTag>());

			if (!collisionEventBufferQuery.IsEmptyIgnoreFilter)
			{
				return;
			}

			Entity eventBufferEntity =
				state.EntityManager.CreateEntity();

			state.EntityManager.AddComponent<CollisionEventBufferTag>(eventBufferEntity);
			state.EntityManager.AddBuffer<CollisionEvent>(eventBufferEntity);
		}

		public void OnUpdate(ref SystemState state)
		{
		}
	}
}