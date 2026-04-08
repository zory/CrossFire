using Core.Physics;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for LinearDampingSystem.
	/// Formula: velocity *= exp(-damping * deltaTime).
	/// Runs second in the integration chain, after all intent systems and before
	/// AngularIntegrationSystem and PositionIntegrationSystem.
	/// </summary>
	public class LinearDampingSystemTests : PhysicsTestBase
	{
		// exp(-1.0f * 1.0f) — used as the reference multiplier in several tests.
		private const float EXP_MINUS_ONE = 0.36787944f;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<LinearDampingSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Core formula: positive damping must reduce velocity by exp(-damping * dt).

		[Test]
		public void OnUpdate_PositiveDamping_ReducesVelocityByExponentialDecay()
		{
			float2 initialVelocity = new float2(10f, 0f);
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, entity, damping: 1.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(initialVelocity * EXP_MINUS_ONE, result);
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Both x and y must be damped — not just one component.

		[Test]
		public void OnUpdate_PositiveDamping_DampsBothVelocityComponents()
		{
			float2 initialVelocity = new float2(6f, 8f);
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, entity, damping: 1.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(initialVelocity * EXP_MINUS_ONE, result);
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Zero damping must leave velocity completely unchanged.

		[Test]
		public void OnUpdate_ZeroDamping_VelocityUnchanged()
		{
			float2 initialVelocity = new float2(5f, 3f);
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, entity, damping: 0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(initialVelocity, result);
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Negative damping is clamped to 0 — the system must never accelerate a body.

		[Test]
		public void OnUpdate_NegativeDamping_ClampedToZero_VelocityUnchanged()
		{
			float2 initialVelocity = new float2(5f, 0f);
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, entity, damping: -5f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(initialVelocity, result, message: "Negative damping should be clamped to 0, leaving velocity unchanged");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Zero velocity must remain zero — damping must not introduce phantom motion.

		[Test]
		public void OnUpdate_ZeroVelocity_RemainsZero()
		{
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, float2.zero);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, entity, damping: 5f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(float2.zero, result);
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Entity without LinearDamping is excluded by the query — velocity must not change.

		[Test]
		public void OnUpdate_EntityWithoutLinearDamping_VelocityUntouched()
		{
			float2 initialVelocity = new float2(7f, 2f);
			// Deliberately no AddLinearDamping call.
			Entity entity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<Velocity>(entity).Value;
			PhysicsAssert.AreEqual(initialVelocity, result, message: "Entity missing LinearDamping should be skipped by the query");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Each entity uses its own damping value — no cross-entity contamination.

		[Test]
		public void OnUpdate_MultipleEntities_EachDampedByOwnValue()
		{
			float2 initialVelocity = new float2(10f, 0f);

			Entity lowDampEntity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, lowDampEntity, damping: 1.0f);

			Entity highDampEntity = PhysicsEntityFactory.CreateEntityWithVelocity(_entityManager, initialVelocity);
			PhysicsEntityFactory.AddLinearDamping(_entityManager, highDampEntity, damping: 2.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 lowDampResult = _entityManager.GetComponentData<Velocity>(lowDampEntity).Value;
			float2 highDampResult = _entityManager.GetComponentData<Velocity>(highDampEntity).Value;

			// exp(-1) ≈ 0.3679,  exp(-2) ≈ 0.1353
			PhysicsAssert.AreEqual(initialVelocity * EXP_MINUS_ONE, lowDampResult, message: "Low damping entity");
			PhysicsAssert.AreEqual(initialVelocity * math.exp(-2f), highDampResult, message: "High damping entity");
		}
	}
}
