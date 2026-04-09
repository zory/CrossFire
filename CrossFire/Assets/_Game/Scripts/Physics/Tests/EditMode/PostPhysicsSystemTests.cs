using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for PostPhysicsSystem.
	/// The system copies WorldPose into LocalTransform each frame:
	///   Position (float2) → LocalTransform.Position (float3, z always 0)
	///   ThetaRad          → LocalTransform.Rotation via quaternion.RotateZ
	/// Scale is not touched.
	/// Only entities carrying both WorldPose and LocalTransform are processed.
	/// </summary>
	public class PostPhysicsSystemTests : PhysicsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<PostPhysicsSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// WorldPose.Position (x, y) must be written to LocalTransform.Position (x, y, 0).

		[Test]
		public void OnUpdate_WorldPosePosition_SyncedToLocalTransform()
		{
			float2 position = new float2(3f, 7f);
			Pose2D pose = new Pose2D { Position = position, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, pose);

			_world.Update();

			float3 result = _entityManager.GetComponentData<LocalTransform>(entity).Position;
			PhysicsAssert.AreEqual(new float3(3f, 7f, 0f), result, message: "Position must be synced from WorldPose");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// The Z component of LocalTransform.Position must always be 0 regardless of pose.

		[Test]
		public void OnUpdate_LocalTransformZ_IsAlwaysZero()
		{
			Pose2D pose = new Pose2D { Position = new float2(5f, -2f), ThetaRad = 1f };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, pose);

			_world.Update();

			float z = _entityManager.GetComponentData<LocalTransform>(entity).Position.z;
			Assert.AreEqual(0f, z, PhysicsAssert.DEFAULT_DELTA, "Z must always be 0");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// WorldPose.ThetaRad must be written to LocalTransform.Rotation as RotateZ.

		[Test]
		public void OnUpdate_WorldPoseThetaRad_SyncedToLocalTransformRotation()
		{
			float theta = math.PI / 4f;
			Pose2D pose = new Pose2D { Position = float2.zero, ThetaRad = theta };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, pose);

			_world.Update();

			quaternion result = _entityManager.GetComponentData<LocalTransform>(entity).Rotation;
			PhysicsAssert.AreEqual(quaternion.RotateZ(theta), result, message: "Rotation must be RotateZ(ThetaRad)");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Zero pose must produce position at origin and identity rotation.

		[Test]
		public void OnUpdate_ZeroPose_ProducesOriginAndIdentityRotation()
		{
			Pose2D pose = new Pose2D { Position = float2.zero, ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, pose);

			_world.Update();

			LocalTransform result = _entityManager.GetComponentData<LocalTransform>(entity);
			PhysicsAssert.AreEqual(float3.zero, result.Position, message: "Zero pose must produce origin position");
			PhysicsAssert.AreEqual(quaternion.identity, result.Rotation, message: "Zero theta must produce identity rotation");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Scale must not be modified — it must retain whatever value it had before.

		[Test]
		public void OnUpdate_Scale_IsNotModified()
		{
			Pose2D pose = new Pose2D { Position = new float2(1f, 2f), ThetaRad = 0f };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, pose);

			float customScale = 3.5f;
			LocalTransform withScale = LocalTransform.Identity;
			withScale.Scale = customScale;
			_entityManager.SetComponentData(entity, withScale);

			_world.Update();

			float resultScale = _entityManager.GetComponentData<LocalTransform>(entity).Scale;
			Assert.AreEqual(customScale, resultScale, PhysicsAssert.DEFAULT_DELTA, "Scale must not be modified");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Entity with WorldPose but no LocalTransform must be excluded by the query.

		[Test]
		public void OnUpdate_EntityWithoutLocalTransform_IsSkipped()
		{
			Pose2D pose = new Pose2D { Position = new float2(1f, 2f), ThetaRad = 0.5f };
			// CreateEntityWithPose gives WorldPose + PrevWorldPose but no LocalTransform.
			Entity entity = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, pose);

			Assert.DoesNotThrow(() => _world.Update(),
				"Entity without LocalTransform must be skipped without error");

			// WorldPose must remain untouched.
			PhysicsAssert.AreEqual(pose, _entityManager.GetComponentData<WorldPose>(entity).Value,
				message: "WorldPose must not be modified");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Multiple entities must each be synced from their own WorldPose independently.

		[Test]
		public void OnUpdate_MultipleEntities_EachSyncedByOwnPose()
		{
			Pose2D poseA = new Pose2D { Position = new float2(1f, 0f), ThetaRad = 0f };
			Pose2D poseB = new Pose2D { Position = new float2(0f, 5f), ThetaRad = math.PI / 2f };

			Entity entityA = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, poseA);
			Entity entityB = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, poseB);

			_world.Update();

			LocalTransform resultA = _entityManager.GetComponentData<LocalTransform>(entityA);
			LocalTransform resultB = _entityManager.GetComponentData<LocalTransform>(entityB);

			PhysicsAssert.AreEqual(new float3(1f, 0f, 0f), resultA.Position, message: "Entity A position");
			PhysicsAssert.AreEqual(quaternion.RotateZ(0f), resultA.Rotation, message: "Entity A rotation");

			PhysicsAssert.AreEqual(new float3(0f, 5f, 0f), resultB.Position, message: "Entity B position");
			PhysicsAssert.AreEqual(quaternion.RotateZ(math.PI / 2f), resultB.Rotation, message: "Entity B rotation");
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// When WorldPose changes between frames the LocalTransform must reflect the
		// new values on the next update.

		[Test]
		public void OnUpdate_PoseChangedBetweenFrames_TransformUpdated()
		{
			Pose2D firstPose = new Pose2D { Position = new float2(1f, 0f), ThetaRad = 0f };
			Pose2D secondPose = new Pose2D { Position = new float2(9f, 4f), ThetaRad = math.PI };

			Entity entity = PhysicsEntityFactory.CreateEntityWithPoseAndTransform(_entityManager, firstPose);

			_world.Update();

			_entityManager.SetComponentData(entity, new WorldPose { Value = secondPose });
			_world.Update();

			LocalTransform result = _entityManager.GetComponentData<LocalTransform>(entity);
			PhysicsAssert.AreEqual(new float3(9f, 4f, 0f), result.Position, message: "Position must reflect updated pose");
			PhysicsAssert.AreEqual(quaternion.RotateZ(math.PI), result.Rotation, message: "Rotation must reflect updated pose");
		}
	}
}
