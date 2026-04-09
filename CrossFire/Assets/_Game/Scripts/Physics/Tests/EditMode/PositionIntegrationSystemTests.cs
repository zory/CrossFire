using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for PositionIntegrationSystem.
	/// Formula: pose.Position += velocity * deltaTime.
	/// Runs fourth in the integration chain, after AngularIntegrationSystem.
	/// Only Position is modified; ThetaRad is untouched.
	/// </summary>
	public class PositionIntegrationSystemTests : PhysicsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<PositionIntegrationSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Positive velocity must advance the position in the velocity direction.

		[Test]
		public void OnUpdate_PositiveVelocity_AdvancesPosition()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			float2 velocity = new float2(3f, 2f);
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, velocity);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			PhysicsAssert.AreEqual(new float2(3f, 2f), result, message: "Positive velocity must advance position");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Negative velocity must retreat the position in the opposite direction.

		[Test]
		public void OnUpdate_NegativeVelocity_RetreatsPosition()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			float2 velocity = new float2(-4f, -1f);
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, velocity);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			PhysicsAssert.AreEqual(new float2(-4f, -1f), result, message: "Negative velocity must retreat position");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Zero velocity must leave the position unchanged.

		[Test]
		public void OnUpdate_ZeroVelocity_PositionUnchanged()
		{
			float2 initialPosition = new float2(5f, 9f);
			Pose2D initialPose = new Pose2D { Position = initialPosition, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, float2.zero);

			SetDeltaTime(1.0f);
			_world.Update();

			float2 result = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			PhysicsAssert.AreEqual(initialPosition, result, message: "Zero velocity must not move position");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// The displacement must be proportional to deltaTime, not a fixed step.

		[Test]
		public void OnUpdate_Integration_IsScaledByDeltaTime()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			float2 velocity = new float2(2f, 0f);

			Entity halfDtEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, velocity);
			Entity fullDtEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, velocity);

			// First update: dt = 0.5 → displacement = 2.0 * 0.5 = 1.0
			SetDeltaTime(0.5f);
			_world.Update();

			float2 halfDtResult = _entityManager.GetComponentData<WorldPose>(halfDtEntity).Value.Position;

			// Freeze halfDtEntity so the second update doesn't accumulate further displacement.
			// Reset fullDtEntity's pose to zero so its second measurement starts clean.
			_entityManager.SetComponentData(halfDtEntity, new Velocity { Value = float2.zero });
			_entityManager.SetComponentData(fullDtEntity, new WorldPose { Value = initialPose });

			// Second update: dt = 1.0 → displacement = 2.0 * 1.0 = 2.0
			SetDeltaTime(1.0f);
			_world.Update();

			float2 fullDtResult = _entityManager.GetComponentData<WorldPose>(fullDtEntity).Value.Position;

			PhysicsAssert.AreEqual(new float2(1f, 0f), halfDtResult, message: "dt=0.5 should give half the displacement");
			PhysicsAssert.AreEqual(new float2(2f, 0f), fullDtResult, message: "dt=1.0 should give full displacement");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Position integration must only affect Position — ThetaRad must be untouched.

		[Test]
		public void OnUpdate_Velocity_DoesNotAffectThetaRad()
		{
			float initialTheta = 1.2f;
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = initialTheta };
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, new float2(5f, 3f));

			SetDeltaTime(1.0f);
			_world.Update();

			float result = _entityManager.GetComponentData<WorldPose>(entity).Value.ThetaRad;
			Assert.AreEqual(initialTheta, result, PhysicsAssert.DEFAULT_DELTA,
				"ThetaRad must not be modified by position integration");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Entity without Velocity is excluded by the query — WorldPose must not change.

		[Test]
		public void OnUpdate_EntityWithoutVelocity_PoseUntouched()
		{
			Pose2D initialPose = new Pose2D { Position = new float2(3f, 4f), ThetaRad = 0.7f };
			// CreateEntityWithPose gives WorldPose + PrevWorldPose but no Velocity.
			Entity entity = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, initialPose);

			SetDeltaTime(1.0f);
			_world.Update();

			WorldPose result = _entityManager.GetComponentData<WorldPose>(entity);
			PhysicsAssert.AreEqual(initialPose, result.Value, message: "Entity without Velocity should be skipped by the query");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Each entity must be displaced by its own velocity — no cross-entity contamination.

		[Test]
		public void OnUpdate_MultipleEntities_EachDisplacedByOwnVelocity()
		{
			Pose2D zeroPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };

			Entity slowEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, zeroPose, new float2(1f, 0f));
			Entity fastEntity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, zeroPose, new float2(5f, 0f));

			SetDeltaTime(1.0f);
			_world.Update();

			float2 slowResult = _entityManager.GetComponentData<WorldPose>(slowEntity).Value.Position;
			float2 fastResult = _entityManager.GetComponentData<WorldPose>(fastEntity).Value.Position;

			PhysicsAssert.AreEqual(new float2(1f, 0f), slowResult, message: "Slow entity");
			PhysicsAssert.AreEqual(new float2(5f, 0f), fastResult, message: "Fast entity");
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// Position must accumulate correctly across two consecutive updates
		// (simulating multiple physics ticks without a pose reset).

		[Test]
		public void OnUpdate_TwoConsecutiveUpdates_PositionAccumulates()
		{
			Pose2D initialPose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			float2 velocity = new float2(3f, 1f);
			Entity entity = PhysicsEntityFactory.CreateDynamicBody(_entityManager, initialPose, velocity);

			SetDeltaTime(1.0f);
			_world.Update();
			_world.Update();

			float2 result = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			// Two ticks at dt=1: displacement = 2 * velocity
			PhysicsAssert.AreEqual(new float2(6f, 2f), result, message: "Position must accumulate over two ticks");
		}
	}
}
