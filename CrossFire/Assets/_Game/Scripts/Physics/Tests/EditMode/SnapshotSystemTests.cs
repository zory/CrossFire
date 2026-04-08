using Core.Physics;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for SnapshotSystem — the first simulation-phase system.
	/// It copies WorldPose.Value into PrevWorldPose.Value each frame so
	/// subsequent systems can compare current vs. previous pose.
	/// </summary>
	public class SnapshotSystemTests : PhysicsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<SnapshotSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Core behaviour: PrevWorldPose must equal WorldPose after the update.

		[Test]
		public void OnUpdate_EntityWithBothComponents_CopiesWorldPoseToPrevWorldPose()
		{
			Pose2D expectedPose = new Pose2D { Position = new float2(3f, 5f), ThetaRad = 1.2f };
			Entity entity = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, expectedPose);

			_world.Update();

			PrevWorldPose prevWorldPose = _entityManager.GetComponentData<PrevWorldPose>(entity);
			PhysicsAssert.AreEqual(expectedPose, prevWorldPose.Value);
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Entity missing PrevWorldPose is excluded by the query — system must not throw.

		[Test]
		public void OnUpdate_EntityWithOnlyWorldPose_IsSkippedWithoutError()
		{
			Pose2D originalPose = new Pose2D { Position = new float2(1f, 0f), ThetaRad = 0f };
			Entity entity = _entityManager.CreateEntity();
			_entityManager.AddComponentData(entity, new WorldPose { Value = originalPose });

			Assert.DoesNotThrow(() => _world.Update());

			WorldPose worldPose = _entityManager.GetComponentData<WorldPose>(entity);
			PhysicsAssert.AreEqual(originalPose, worldPose.Value);
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Each entity gets its own WorldPose copied — no cross-entity contamination.

		[Test]
		public void OnUpdate_MultipleEntities_EachGetTheirOwnWorldPoseCopied()
		{
			Pose2D poseA = new Pose2D { Position = new float2(1f, 0f), ThetaRad = 0.1f };
			Pose2D poseB = new Pose2D { Position = new float2(0f, 2f), ThetaRad = 0.5f };
			Pose2D poseC = new Pose2D { Position = new float2(-3f, -1f), ThetaRad = 3.0f };

			Entity entityA = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, poseA);
			Entity entityB = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, poseB);
			Entity entityC = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, poseC);

			_world.Update();

			PhysicsAssert.AreEqual(poseA, _entityManager.GetComponentData<PrevWorldPose>(entityA).Value, message: "Entity A");
			PhysicsAssert.AreEqual(poseB, _entityManager.GetComponentData<PrevWorldPose>(entityB).Value, message: "Entity B");
			PhysicsAssert.AreEqual(poseC, _entityManager.GetComponentData<PrevWorldPose>(entityC).Value, message: "Entity C");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Snapshot is point-in-time: each update captures the WorldPose at that moment.

		[Test]
		public void OnUpdate_WorldPoseChangedBetweenUpdates_PrevWorldPoseReflectsValueAtTimeOfUpdate()
		{
			Pose2D firstPose = new Pose2D { Position = new float2(1f, 0f), ThetaRad = 0f };
			Pose2D secondPose = new Pose2D { Position = new float2(9f, 9f), ThetaRad = 2f };

			Entity entity = PhysicsEntityFactory.CreateEntityWithPose(_entityManager, firstPose);

			_world.Update();

			PhysicsAssert.AreEqual(firstPose, _entityManager.GetComponentData<PrevWorldPose>(entity).Value, message: "After first update");

			_entityManager.SetComponentData(entity, new WorldPose { Value = secondPose });

			_world.Update();

			PhysicsAssert.AreEqual(secondPose, _entityManager.GetComponentData<PrevWorldPose>(entity).Value, message: "After second update");
		}
	}
}
