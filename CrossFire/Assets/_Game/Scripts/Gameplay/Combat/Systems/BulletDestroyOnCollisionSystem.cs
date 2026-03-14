using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct BulletDestroyOnCollisionSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			Entity collisionEventBufferEntity = SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();
			DynamicBuffer<CollisionEvent> collisionEvents = entityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);

			//NativeArray<CollisionEvent> collisionEventsCopy = collisionEvents.ToNativeArray(Allocator.Temp);
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			for (int index = 0; index < collisionEvents.Length; index++)
			{
				CollisionEvent collisionEvent = collisionEvents[index];

				DestroyBulletIfNeeded(
					entityManager,
					entityCommandBuffer,
					collisionEvent.FirstEntity,
					collisionEvent.SecondEntity
				);

				DestroyBulletIfNeeded(
					entityManager,
					entityCommandBuffer,
					collisionEvent.SecondEntity,
					collisionEvent.FirstEntity
				);
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
			//collisionEventsCopy.Dispose();
		}

		private static void DestroyBulletIfNeeded(
			EntityManager entityManager,
			EntityCommandBuffer entityCommandBuffer,
			Entity possibleBulletEntity,
			Entity otherEntity)
		{
			if (!entityManager.Exists(possibleBulletEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<BulletTag>(possibleBulletEntity))
			{
				return;
			}

			if (entityManager.HasComponent<Owner>(possibleBulletEntity))
			{
				Owner owner = entityManager.GetComponentData<Owner>(possibleBulletEntity);
				if (owner.Value == otherEntity)
				{
					return;
				}
			}

			entityCommandBuffer.DestroyEntity(possibleBulletEntity);
		}
	}
}