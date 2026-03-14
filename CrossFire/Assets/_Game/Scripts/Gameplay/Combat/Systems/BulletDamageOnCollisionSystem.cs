using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Combat
{
	[BurstCompile]
	[DisableAutoCreation]
	public partial struct BulletDamageOnCollisionSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CrossFire.Physics.CollisionEventBufferTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			Entity collisionEventBufferEntity = SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();
			DynamicBuffer<CollisionEvent> collisionEvents = entityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);

			for (int collisionEventIndex = 0; collisionEventIndex < collisionEvents.Length; collisionEventIndex++)
			{
				CollisionEvent collisionEvent = collisionEvents[collisionEventIndex];

				ApplyBulletDamageIfPossible(
					entityManager,
					collisionEvent.FirstEntity,
					collisionEvent.SecondEntity);

				ApplyBulletDamageIfPossible(
					entityManager,
					collisionEvent.SecondEntity,
					collisionEvent.FirstEntity);
			}
		}

		private static void ApplyBulletDamageIfPossible(
			EntityManager entityManager,
			Entity possibleBulletEntity,
			Entity possibleTargetEntity)
		{
			if (entityManager.HasComponent<Owner>(possibleBulletEntity))
			{
				Owner bulletOwner = entityManager.GetComponentData<Owner>(possibleBulletEntity);
				if (bulletOwner.Value == possibleTargetEntity)
				{
					return;
				}
			}

			bool entityIsBullet = entityManager.HasComponent<BulletTag>(possibleBulletEntity);
			if (!entityIsBullet)
			{
				return;
			}

			bool bulletHasDamage = entityManager.HasComponent<BulletDamage>(possibleBulletEntity);
			if (!bulletHasDamage)
			{
				return;
			}

			bool targetHasHealth = entityManager.HasComponent<Health>(possibleTargetEntity);
			if (!targetHasHealth)
			{
				return;
			}

			BulletDamage bulletDamage = entityManager.GetComponentData<BulletDamage>(possibleBulletEntity);
			Health targetHealth = entityManager.GetComponentData<Health>(possibleTargetEntity);
			targetHealth.Value -= bulletDamage.Value;

			entityManager.SetComponentData(
				possibleTargetEntity,
				targetHealth);
		}
	}
}