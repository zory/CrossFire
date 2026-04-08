using Core.Physics;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for AngularIntegrationSystem.
	/// Formula: pose.ThetaRad += angularVelocity * deltaTime.
	/// Runs third in the integration chain, after LinearDampingSystem and before
	/// PositionIntegrationSystem. Only ThetaRad is modified; position is untouched.
	/// </summary>
	public class AngularIntegrationSystemTests : PhysicsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<AngularIntegrationSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Positive angular velocity must increase ThetaRad (CCW rotation).

		[Test]
		public void OnUpdate_PositiveAngularVelocity_IncreasesThetaRad()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity: 1.5f);

			SetDeltaTime(1.0f);
			_world.Update();

			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			Assert.AreEqual(1.5f, result, PhysicsAssert.DEFAULT_DELTA);
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Negative angular velocity must decrease ThetaRad (CW rotation).

		[Test]
		public void OnUpdate_NegativeAngularVelocity_DecreasesThetaRad()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity: -2.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			Assert.AreEqual(-2.0f, result, PhysicsAssert.DEFAULT_DELTA);
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Zero angular velocity must leave ThetaRad unchanged.

		[Test]
		public void OnUpdate_ZeroAngularVelocity_ThetaRadUnchanged()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 1.0f };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity: 0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			Assert.AreEqual(1.0f, result, PhysicsAssert.DEFAULT_DELTA);
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// The angle change must be proportional to deltaTime, not a fixed step.

		[Test]
		public void OnUpdate_Integration_IsScaledByDeltaTime()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			// Use 2.0 rad/s so both results (1.0 and 2.0) stay inside [-π, π] and are not wrapped.
			float angularVelocity = 2.0f;

			Entity halfDtEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity);
			Entity fullDtEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity);

			// First update: dt = 0.5 → delta = 2.0 * 0.5 = 1.0
			SetDeltaTime(0.5f);
			_world.Update();

			float halfDtResult = _entityManager.GetComponentData<WorldPose>(halfDtEntity).Value.ThetaRad;

			// Freeze halfDtEntity so the second update doesn't accumulate further rotation.
			// Reset fullDtEntity's pose to zero so its second measurement starts clean.
			_entityManager.SetComponentData(halfDtEntity, new AngularVelocity { Value = 0f });
			_entityManager.SetComponentData(fullDtEntity, new WorldPose { Value = initialPose });

			// Second update: dt = 1.0 → delta = 2.0 * 1.0 = 2.0
			SetDeltaTime(1.0f);
			_world.Update();

			float fullDtResult = _entityManager.GetComponentData<WorldPose>(fullDtEntity).Value.ThetaRad;

			Assert.AreEqual(1.0f, halfDtResult, PhysicsAssert.DEFAULT_DELTA, "dt=0.5 should give half the rotation");
			Assert.AreEqual(2.0f, fullDtResult, PhysicsAssert.DEFAULT_DELTA, "dt=1.0 should give full rotation");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Angular integration must only affect ThetaRad — position must be untouched.

		[Test]
		public void OnUpdate_AngularVelocity_DoesNotAffectPosition()
		{
			float2 initialPosition = new float2(3f, 7f);
			Pose2D initialPose = new Pose2D { Position = initialPosition, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero, angularVelocity: 5.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 resultPosition = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			PhysicsAssert.AreEqual(initialPosition, resultPosition, message: "Position must not be modified by angular integration");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Entity without AngularVelocity is excluded by the query — WorldPose must not change.

		[Test]
		public void OnUpdate_EntityWithoutAngularVelocity_PoseUntouched()
		{
			Pose2D initialPose = new Pose2D { Position = new float2(1f, 2f), ThetaRad = 0.5f };
			// CreateEntityWithPose gives WorldPose + PrevWorldPose but no AngularVelocity.
			Entity entity = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, initialPose);

			SetDeltaTime(1.0f);
			_world.Update();

			WorldPose result = _entityManager.GetComponentData<WorldPose>(entity);
			PhysicsAssert.AreEqual(initialPose, result.Value, message: "Entity without AngularVelocity should be skipped by the query");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Each entity rotates by its own angular velocity — no cross-entity contamination.

		[Test]
		public void OnUpdate_MultipleEntities_EachRotatedByOwnAngularVelocity()
		{
			Pose2D zeroPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };

			Entity slowEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, zeroPose, float2.zero, angularVelocity: 1.0f);
			Entity fastEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, zeroPose, float2.zero, angularVelocity: 3.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			float slowResult = _entityManager.GetComponentData<WorldPose>(slowEntity).Value.ThetaRad;
			float fastResult = _entityManager.GetComponentData<WorldPose>(fastEntity).Value.ThetaRad;

			Assert.AreEqual(1.0f, slowResult, PhysicsAssert.DEFAULT_DELTA, "Slow entity");
			Assert.AreEqual(3.0f, fastResult, PhysicsAssert.DEFAULT_DELTA, "Fast entity");
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// Angle exceeding π must be wrapped back into [-π, π].
		// Prevents float32 precision loss during long play sessions.

		[Test]
		public void OnUpdate_ThetaExceedsPi_IsWrappedToMinusPiPiRange()
		{
			// Start just below π, add enough angular velocity to cross it.
			float initialTheta = math.PI - 0.5f;
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = initialTheta };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(
				_entityManager, initialPose, float2.zero, angularVelocity: 2.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			// Raw: (π - 0.5) + 2.0 = π + 1.5  →  wrapped: π + 1.5 - 2π = 1.5 - π
			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			float expected = initialTheta + 2.0f - math.PI * 2f;
			Assert.AreEqual(expected, result, PhysicsAssert.DEFAULT_DELTA,
				"Angle exceeding π must wrap to [-π, π]");
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// Angle dropping below -π must also wrap back into [-π, π].

		[Test]
		public void OnUpdate_ThetaBelowMinusPi_IsWrappedToMinusPiPiRange()
		{
			// Start just above -π, subtract enough to cross it.
			float initialTheta = -math.PI + 0.5f;
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = initialTheta };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(
				_entityManager, initialPose, float2.zero, angularVelocity: -2.0f);

			SetDeltaTime(1.0f);
			_world.Update();

			// Raw: (-π + 0.5) - 2.0 = -π - 1.5  →  wrapped: -π - 1.5 + 2π = π - 1.5
			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			float expected = initialTheta - 2.0f + math.PI * 2f;
			Assert.AreEqual(expected, result, PhysicsAssert.DEFAULT_DELTA,
				"Angle dropping below -π must wrap to [-π, π]");
		}
	}
}
