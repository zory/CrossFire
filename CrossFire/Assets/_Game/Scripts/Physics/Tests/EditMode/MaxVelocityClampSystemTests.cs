using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for MaxVelocityClampSystem.
	/// The system clamps the Velocity magnitude to MaxVelocity.
	/// Direction is preserved; only the magnitude is reduced when over the limit.
	/// Negative or zero MaxVelocity is treated as zero, pinning the entity.
	/// The system is time-independent — deltaTime is not involved.
	/// </summary>
	public class MaxVelocityClampSystemTests : PhysicsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<MaxVelocityClampSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Velocity whose magnitude exceeds the limit must be scaled down to exactly
		// the limit magnitude.

		[Test]
		public void OnUpdate_VelocityExceedsMax_MagnitudeClampedToMax()
		{
			// velocity (6, 8) has magnitude 10; limit is 5 → expected magnitude 5.
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(6f, 8f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, 5f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			Assert.AreEqual(5f, math.length(result), PhysicsAssert.DEFAULT_DELTA,
				"Magnitude must be clamped to MaxVelocity");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// When clamping, the velocity direction must be preserved — only magnitude changes.

		[Test]
		public void OnUpdate_VelocityExceedsMax_DirectionPreserved()
		{
			// velocity (6, 8) → normalised direction (0.6, 0.8); after clamping to 5 → (3, 4).
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(6f, 8f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, 5f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(new float2(3f, 4f), result, message: "Direction must be preserved when clamping");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Velocity whose magnitude is below the limit must be left completely unchanged.

		[Test]
		public void OnUpdate_VelocityBelowMax_VelocityUnchanged()
		{
			float2 velocity = new float2(1f, 2f);
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, velocity);
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, 10f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(velocity, result, message: "Velocity below max must not be modified");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Zero velocity must pass through without producing NaN or infinity
		// (guards against division by zero in the normalisation path).

		[Test]
		public void OnUpdate_ZeroVelocity_RemainsZero()
		{
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, float2.zero);
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, 5f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(float2.zero, result, message: "Zero velocity must remain zero");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// MaxVelocity of zero must pin the entity — any meaningful velocity is clamped to zero.

		[Test]
		public void OnUpdate_MaxVelocityZero_VelocityClampedToZero()
		{
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(5f, 0f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, 0f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(float2.zero, result, message: "MaxVelocity=0 must pin the entity");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Negative MaxVelocity must be treated the same as zero — entity is pinned.

		[Test]
		public void OnUpdate_NegativeMaxVelocity_TreatedAsZero()
		{
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(5f, 0f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entity, -10f);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(float2.zero, result, message: "Negative MaxVelocity must be treated as zero");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Entity without MaxVelocity is excluded by the query — velocity must not change.

		[Test]
		public void OnUpdate_EntityWithoutMaxVelocity_VelocityUnchanged()
		{
			float2 velocity = new float2(100f, 200f);
			// CreateEntityWithVelocity gives only Velocity, no MaxVelocity.
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, velocity);

			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(velocity, result, message: "Entity without MaxVelocity must be skipped by the query");
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// Each entity must be clamped by its own limit — no cross-entity contamination.

		[Test]
		public void OnUpdate_MultipleEntities_EachClampedByOwnLimit()
		{
			// Entity A: velocity 20, limit 10 → clamped to 10.
			Entity entityA = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(20f, 0f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entityA, 10f);

			// Entity B: velocity 3, limit 10 → unchanged.
			Entity entityB = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(3f, 0f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entityB, 10f);

			// Entity C: velocity 5, limit 2 → clamped to 2.
			Entity entityC = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, new float2(5f, 0f));
			PhysicsEntityFactory.AddMaxVelocity(_entityManager, entityC, 2f);

			_world.Update();

			float2 resultA = _entityManager.GetComponentData<Velocity>(entityA).Value;
			float2 resultB = _entityManager.GetComponentData<Velocity>(entityB).Value;
			float2 resultC = _entityManager.GetComponentData<Velocity>(entityC).Value;

			Assert.AreEqual(10f, math.length(resultA), PhysicsAssert.DEFAULT_DELTA, "Entity A must be clamped to 10");
			PhysicsAssert.AreEqual(new float2(3f, 0f), resultB, message: "Entity B must be unchanged");
			Assert.AreEqual(2f, math.length(resultC), PhysicsAssert.DEFAULT_DELTA, "Entity C must be clamped to 2");
		}
	}
}
