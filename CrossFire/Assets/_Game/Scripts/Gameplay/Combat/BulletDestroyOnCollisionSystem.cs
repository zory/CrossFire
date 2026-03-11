using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Combat
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(BulletDamageOnCollisionSystem))]
	[UpdateBefore(typeof(CollisionEventCleanupSystem))]
	public partial struct BulletDestroyOnCollisionSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity collisionEventBufferEntity =
				SystemAPI.GetSingletonEntity<CrossFire.Physics.CollisionEventBufferTag>();

			DynamicBuffer<CrossFire.Physics.CollisionEvent> collisionEventBuffer =
				entityManager.GetBuffer<CrossFire.Physics.CollisionEvent>(collisionEventBufferEntity);

			NativeArray<CrossFire.Physics.CollisionEvent> collisionEventsCopy =
				collisionEventBuffer.ToNativeArray(Allocator.Temp);

			EntityCommandBuffer entityCommandBuffer =
				new EntityCommandBuffer(Allocator.Temp);

			for (int collisionEventIndex = 0;
				 collisionEventIndex < collisionEventsCopy.Length;
				 collisionEventIndex++)
			{
				CrossFire.Physics.CollisionEvent collisionEvent =
					collisionEventsCopy[collisionEventIndex];

				DestroyBulletIfNeeded(
					entityManager,
					entityCommandBuffer,
					collisionEvent.FirstEntity,
					collisionEvent.SecondEntity);

				DestroyBulletIfNeeded(
					entityManager,
					entityCommandBuffer,
					collisionEvent.SecondEntity,
					collisionEvent.FirstEntity);
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
			collisionEventsCopy.Dispose();
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
				Owner bulletOwner =
					entityManager.GetComponentData<Owner>(possibleBulletEntity);

				if (bulletOwner.Value == otherEntity)
				{
					return;
				}
			}

			entityCommandBuffer.DestroyEntity(possibleBulletEntity);
		}
	}
}