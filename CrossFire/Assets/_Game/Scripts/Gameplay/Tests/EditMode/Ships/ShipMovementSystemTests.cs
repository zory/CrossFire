using Core.Physics;
using CrossFire.Core;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships.Tests.EditMode
{
	/// <summary>
	/// Tests for <see cref="ShipMovementSystem"/>.
	///
	/// Forward vector at theta=0 is (0, 1) — ship faces +Y by default.
	/// Formula: velocity += forward * acceleration * thrust * deltaTime
	/// Angular velocity is set directly: angularVelocity = turn * turnSpeed (no integration here).
	/// </summary>
	public class ShipMovementSystemTests : ShipsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<ShipMovementSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Zero thrust must leave velocity completely unchanged.

		[Test]
		public void OnUpdate_ZeroThrust_VelocityUnchanged()
		{
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(_entityManager);
			ShipsEntityFactory.SetIntent(_entityManager, entity, thrust: 0f, turn: 0f);

			SetDeltaTime(1f);
			_world.Update();

			float2 velocity = _entityManager.GetComponentData<Velocity>(entity).Value;
			Assert.AreEqual(0f, velocity.x, 1e-5f, "velocity.x");
			Assert.AreEqual(0f, velocity.y, 1e-5f, "velocity.y");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Positive thrust must accelerate the ship along its forward vector using
		// ThrustAcceleration. At theta=0, forward = (0, 1), so only Y changes.

		[Test]
		public void OnUpdate_PositiveThrust_AddsVelocityAlongForward()
		{
			// theta=0 → forward=(0,1); thrustAcc=10; thrust=1; dt=0.5 → delta=(0, 5)
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thetaRad: 0f, thrustAcc: 10f);
			ShipsEntityFactory.SetIntent(_entityManager, entity, thrust: 1f);

			SetDeltaTime(0.5f);
			_world.Update();

			float2 velocity = _entityManager.GetComponentData<Velocity>(entity).Value;
			Assert.AreEqual(0f, velocity.x, 1e-5f, "No lateral component expected");
			Assert.AreEqual(5f, velocity.y, 1e-5f, "10 * 1 * 0.5 = 5");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Negative thrust must decelerate using BrakeAcceleration (not ThrustAcceleration).
		// At theta=0, thrust=-1, brakeAcc=5, dt=1 → velocity += (0,1) * 5 * -1 * 1 = (0,-5).

		[Test]
		public void OnUpdate_NegativeThrust_UseBrakeAcceleration()
		{
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thetaRad: 0f, thrustAcc: 10f, brakeAcc: 5f);
			ShipsEntityFactory.SetIntent(_entityManager, entity, thrust: -1f);

			SetDeltaTime(1f);
			_world.Update();

			float2 velocity = _entityManager.GetComponentData<Velocity>(entity).Value;
			Assert.AreEqual(0f,  velocity.x, 1e-5f, "No lateral component expected");
			Assert.AreEqual(-5f, velocity.y, 1e-5f, "BrakeAcc(5) * thrust(-1) * dt(1) = -5");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Turn intent must set AngularVelocity = turn * turnSpeed (direct assignment, no
		// integration here). Previous angular velocity is overwritten each frame.

		[Test]
		public void OnUpdate_TurnIntent_SetsAngularVelocityDirectly()
		{
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, turnSpeed: 3f);
			ShipsEntityFactory.SetIntent(_entityManager, entity, turn: 0.5f);

			SetDeltaTime(1f);
			_world.Update();

			float angularVelocity = _entityManager.GetComponentData<AngularVelocity>(entity).Value;
			Assert.AreEqual(1.5f, angularVelocity, 1e-5f, "0.5 * 3 = 1.5");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Thrust values above 1 are clamped to 1 — a value of 2 should produce the same
		// result as a value of 1.

		[Test]
		public void OnUpdate_ThrustAboveOne_ClampedToOne()
		{
			Entity entityClamped = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thrustAcc: 10f);
			Entity entityNormal  = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thrustAcc: 10f);

			ShipsEntityFactory.SetIntent(_entityManager, entityClamped, thrust: 2f);
			ShipsEntityFactory.SetIntent(_entityManager, entityNormal,  thrust: 1f);

			SetDeltaTime(1f);
			_world.Update();

			float2 clampedVelocity = _entityManager.GetComponentData<Velocity>(entityClamped).Value;
			float2 normalVelocity  = _entityManager.GetComponentData<Velocity>(entityNormal).Value;

			Assert.AreEqual(normalVelocity.y, clampedVelocity.y, 1e-5f,
				"Thrust clamped to 1 must equal thrust of exactly 1");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Turn values above 1 are clamped — angular velocity must not exceed turnSpeed.

		[Test]
		public void OnUpdate_TurnAboveOne_ClampedToOne()
		{
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, turnSpeed: 3f);
			ShipsEntityFactory.SetIntent(_entityManager, entity, turn: 5f);

			SetDeltaTime(1f);
			_world.Update();

			float angularVelocity = _entityManager.GetComponentData<AngularVelocity>(entity).Value;
			Assert.AreEqual(3f, angularVelocity, 1e-5f,
				"Turn clamped to 1 → angularVelocity = 1 * turnSpeed(3)");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// DeltaTime scales thrust contribution linearly — halving dt halves the velocity delta.

		[Test]
		public void OnUpdate_DeltaTimeScalesThrustContribution()
		{
			Entity entityHalfDt = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thrustAcc: 10f);
			Entity entityFullDt = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thrustAcc: 10f);

			ShipsEntityFactory.SetIntent(_entityManager, entityHalfDt, thrust: 1f);
			ShipsEntityFactory.SetIntent(_entityManager, entityFullDt, thrust: 1f);

			// Both are in the same world — run at dt=0.5, check half-result, then at dt=0.5 again
			// and compare the total to a full-dt run.  Easier: two separate worlds would be ideal
			// but within one world we just verify the formula at dt=0.5.
			SetDeltaTime(0.5f);
			_world.Update();

			float2 halfDtVelocity = _entityManager.GetComponentData<Velocity>(entityHalfDt).Value;
			// 10 * 1 * 0.5 = 5 in the Y direction
			Assert.AreEqual(5f, halfDtVelocity.y, 1e-5f, "10 * 1 * 0.5 = 5");
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// Forward direction depends on ThetaRad. At theta = PI/2 (facing +X),
		// forward = (-sin(PI/2), cos(PI/2)) = (-1, 0), so thrust adds to -X.

		[Test]
		public void OnUpdate_ThetaRad_ForwardDirectionRotatesCorrectly()
		{
			// theta = PI/2 → forward = (-1, 0)
			Entity entity = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thetaRad: math.PI * 0.5f, thrustAcc: 10f);
			ShipsEntityFactory.SetIntent(_entityManager, entity, thrust: 1f);

			SetDeltaTime(1f);
			_world.Update();

			float2 velocity = _entityManager.GetComponentData<Velocity>(entity).Value;
			Assert.AreEqual(-10f, velocity.x, 1e-4f, "forward.x = -sin(PI/2) = -1 → vx = -10");
			Assert.AreEqual(0f,   velocity.y, 1e-4f, "forward.y = cos(PI/2) ≈ 0 → vy ≈ 0");
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// An entity missing any required component (e.g. BrakeAcceleration) is excluded
		// by the query and must not be processed.

		[Test]
		public void OnUpdate_EntityMissingRequiredComponent_Skipped()
		{
			// Create entity without BrakeAcceleration — excluded from the system query.
			Entity entity = _entityManager.CreateEntity();
			_entityManager.AddComponentData(entity, new WorldPose
				{ Value = new Pose2D { ThetaRad = 0f } });
			_entityManager.AddComponentData(entity, new Velocity { Value = float2.zero });
			_entityManager.AddComponentData(entity, new AngularVelocity { Value = 0f });
			_entityManager.AddComponentData(entity, new ControlIntent { Thrust = 1f, Turn = 1f });
			_entityManager.AddComponentData(entity, new ThrustAcceleration { Value = 10f });
			_entityManager.AddComponentData(entity, new TurnSpeed { Value = 5f });
			// BrakeAcceleration intentionally omitted

			SetDeltaTime(1f);
			_world.Update();

			float2 velocity      = _entityManager.GetComponentData<Velocity>(entity).Value;
			float angularVelocity = _entityManager.GetComponentData<AngularVelocity>(entity).Value;

			Assert.AreEqual(0f, velocity.x,      1e-5f, "velocity.x must be untouched");
			Assert.AreEqual(0f, velocity.y,      1e-5f, "velocity.y must be untouched");
			Assert.AreEqual(0f, angularVelocity, 1e-5f, "angularVelocity must be untouched");
		}

		// ─── Test 10 ──────────────────────────────────────────────────────────────
		// Multiple entities must each respond to their own intent independently.

		[Test]
		public void OnUpdate_MultipleEntities_EachUsesOwnIntent()
		{
			Entity forward = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thetaRad: 0f, thrustAcc: 10f);
			Entity backward = ShipsEntityFactory.CreateShipWithMovementComponents(
				_entityManager, thetaRad: 0f, brakeAcc: 4f);

			ShipsEntityFactory.SetIntent(_entityManager, forward,  thrust:  1f);
			ShipsEntityFactory.SetIntent(_entityManager, backward, thrust: -1f);

			SetDeltaTime(1f);
			_world.Update();

			float2 forwardVelocity  = _entityManager.GetComponentData<Velocity>(forward).Value;
			float2 backwardVelocity = _entityManager.GetComponentData<Velocity>(backward).Value;

			Assert.AreEqual( 10f, forwardVelocity.y,  1e-5f, "Forward ship: thrustAcc(10)*1*1=10");
			Assert.AreEqual( -4f, backwardVelocity.y, 1e-5f, "Backward ship: brakeAcc(4)*-1*1=-4");
		}
	}
}
