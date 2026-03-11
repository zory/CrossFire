using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Combat
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(CrossFire.Physics.CollisionDetectionSystem))]
	[UpdateBefore(typeof(CrossFire.Combat.BulletDestroyOnCollisionSystem))]
	[UpdateBefore(typeof(CrossFire.Physics.CollisionEventCleanupSystem))]
	public partial struct BulletDamageOnCollisionSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CrossFire.Physics.CollisionEventBufferTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity collisionEventBufferEntity =
				SystemAPI.GetSingletonEntity<CrossFire.Physics.CollisionEventBufferTag>();

			DynamicBuffer<CrossFire.Physics.CollisionEvent> collisionEvents =
				entityManager.GetBuffer<CrossFire.Physics.CollisionEvent>(collisionEventBufferEntity);

			for (int collisionEventIndex = 0;
				 collisionEventIndex < collisionEvents.Length;
				 collisionEventIndex++)
			{
				CrossFire.Physics.CollisionEvent collisionEvent =
					collisionEvents[collisionEventIndex];

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
				Owner bulletOwner =
					entityManager.GetComponentData<Owner>(possibleBulletEntity);

				if (bulletOwner.Value == possibleTargetEntity)
				{
					return;
				}
			}

			bool entityIsBullet =
				entityManager.HasComponent<BulletTag>(possibleBulletEntity);

			if (!entityIsBullet)
			{
				return;
			}

			bool bulletHasDamage =
				entityManager.HasComponent<BulletDamage>(possibleBulletEntity);

			if (!bulletHasDamage)
			{
				return;
			}

			bool targetHasHealth =
				entityManager.HasComponent<Health>(possibleTargetEntity);

			if (!targetHasHealth)
			{
				return;
			}

			BulletDamage bulletDamage =
				entityManager.GetComponentData<BulletDamage>(possibleBulletEntity);

			Health targetHealth =
				entityManager.GetComponentData<Health>(possibleTargetEntity);

			targetHealth.Value -= bulletDamage.Value;

			entityManager.SetComponentData(
				possibleTargetEntity,
				targetHealth);
		}
	}
}