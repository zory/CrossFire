using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Factory helpers for creating ECS entities with common Core.Physics component
	/// combinations. Keeps individual test methods free of repetitive AddComponentData calls.
	/// </summary>
	public static class PhysicsEntityFactory
	{
		/// <summary>
		/// Entity with WorldPose and PrevWorldPose. Required by SnapshotSystem and all
		/// downstream systems that read WorldPose.
		/// </summary>
		public static Entity CreateEntityWithPose(EntityManager entityManager, Pose2D pose)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new WorldPose { Value = pose });
			entityManager.AddComponentData(entity, new PrevWorldPose { Value = default });
			return entity;
		}

		/// <summary>
		/// Entity with only a Velocity component. Used by systems that operate on
		/// velocity alone (e.g. LinearDampingSystem, MaxVelocityClampSystem).
		/// </summary>
		public static Entity CreateEntityWithVelocity(EntityManager entityManager, float2 velocity)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new Velocity { Value = velocity });
			return entity;
		}

		/// <summary>
		/// Entity with WorldPose, PrevWorldPose, Velocity and AngularVelocity.
		/// The standard moving body used by integration systems.
		/// </summary>
		public static Entity CreateDynamicBody(
			EntityManager entityManager,
			Pose2D pose,
			float2 velocity,
			float angularVelocity = 0f)
		{
			Entity entity = CreateEntityWithPose(entityManager, pose);
			entityManager.AddComponentData(entity, new Velocity { Value = velocity });
			entityManager.AddComponentData(entity, new AngularVelocity { Value = angularVelocity });
			return entity;
		}

		/// <summary>
		/// Adds LinearDamping to an existing entity.
		/// </summary>
		public static void AddLinearDamping(EntityManager entityManager, Entity entity, float damping)
		{
			entityManager.AddComponentData(entity, new LinearDamping { Value = damping });
		}

		/// <summary>
		/// Adds MaxVelocity to an existing entity.
		/// </summary>
		public static void AddMaxVelocity(EntityManager entityManager, Entity entity, float maxVelocity)
		{
			entityManager.AddComponentData(entity, new MaxVelocity { Value = maxVelocity });
		}
	}
}
