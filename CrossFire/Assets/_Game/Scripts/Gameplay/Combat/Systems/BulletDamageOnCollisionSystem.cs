using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct BulletDamageOnCollisionSystem : ISystem
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

			for (int index = 0; index < collisionEvents.Length; index++)
			{
				CollisionEvent collisionEvent = collisionEvents[index];

				ApplyBulletDamageIfPossible(
					entityManager,
					collisionEvent.FirstEntity,
					collisionEvent.SecondEntity
				);

				ApplyBulletDamageIfPossible(
					entityManager,
					collisionEvent.SecondEntity,
					collisionEvent.FirstEntity
				);
			}
		}

		private static void ApplyBulletDamageIfPossible(
			EntityManager entityManager,
			Entity possibleBulletEntity,
			Entity possibleTargetEntity)
		{
			if (!entityManager.Exists(possibleBulletEntity) || !entityManager.Exists(possibleTargetEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<BulletTag>(possibleBulletEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<BulletDamage>(possibleBulletEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<Health>(possibleTargetEntity))
			{
				return;
			}

			if (entityManager.HasComponent<Owner>(possibleBulletEntity))
			{
				Owner owner = entityManager.GetComponentData<Owner>(possibleBulletEntity);
				if (owner.Value == possibleTargetEntity)
				{
					return;
				}
			}

			BulletDamage bulletDamage = entityManager.GetComponentData<BulletDamage>(possibleBulletEntity);

			Health health = entityManager.GetComponentData<Health>(possibleTargetEntity);

			health.Value -= bulletDamage.Value;

			entityManager.SetComponentData(possibleTargetEntity, health);
		}
	}
}