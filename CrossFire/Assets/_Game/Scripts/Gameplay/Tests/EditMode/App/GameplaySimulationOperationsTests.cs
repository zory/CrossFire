using Core.Physics;
using CrossFire.App;
using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Ships;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrossFire.Tests.EditMode
{
	/// <summary>
	/// Tests for the reusable ECS primitives in <see cref="GameplaySimulationOperations"/>.
	/// Covers entity destruction, pose application, and ship/bullet capture.
	/// Spawn operations are not tested here because they require a prefab registry
	/// that is impractical to wire up in edit-mode without a full scene.
	/// </summary>
	public class GameplaySimulationOperationsTests
	{
		private World _world;
		private EntityManager _em;

		[SetUp]
		public void SetUp()
		{
			_world = new World("GameplayOpsTestWorld");
			_em = _world.EntityManager;
		}

		[TearDown]
		public void TearDown()
		{
			_world.Dispose();
		}

		// ─── DestroyAllShips ──────────────────────────────────────────────────

		[Test]
		public void DestroyAllShips_WithShipEntities_RemovesAllShips()
		{
			_em.CreateEntity(ComponentType.ReadWrite<ShipTag>());
			_em.CreateEntity(ComponentType.ReadWrite<ShipTag>());
			_em.CreateEntity(ComponentType.ReadWrite<ShipTag>());

			GameplaySimulationOperations.DestroyAllShips(_em);

			using EntityQuery query = _em.CreateEntityQuery(ComponentType.ReadOnly<ShipTag>());
			Assert.AreEqual(0, query.CalculateEntityCount());
		}

		[Test]
		public void DestroyAllShips_WithNoEntities_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GameplaySimulationOperations.DestroyAllShips(_em));
		}

		[Test]
		public void DestroyAllShips_DoesNotDestroyNonShipEntities()
		{
			_em.CreateEntity(ComponentType.ReadWrite<ShipTag>());
			Entity other = _em.CreateEntity(ComponentType.ReadWrite<BulletTag>());

			GameplaySimulationOperations.DestroyAllShips(_em);

			Assert.IsTrue(_em.Exists(other), "Non-ship entity must survive DestroyAllShips");
		}

		// ─── DestroyAllBullets ────────────────────────────────────────────────

		[Test]
		public void DestroyAllBullets_WithBulletEntities_RemovesAllBullets()
		{
			_em.CreateEntity(ComponentType.ReadWrite<BulletTag>());
			_em.CreateEntity(ComponentType.ReadWrite<BulletTag>());

			GameplaySimulationOperations.DestroyAllBullets(_em);

			using EntityQuery query = _em.CreateEntityQuery(ComponentType.ReadOnly<BulletTag>());
			Assert.AreEqual(0, query.CalculateEntityCount());
		}

		[Test]
		public void DestroyAllBullets_WithNoEntities_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GameplaySimulationOperations.DestroyAllBullets(_em));
		}

		[Test]
		public void DestroyAllBullets_DoesNotDestroyNonBulletEntities()
		{
			_em.CreateEntity(ComponentType.ReadWrite<BulletTag>());
			Entity other = _em.CreateEntity(ComponentType.ReadWrite<ShipTag>());

			GameplaySimulationOperations.DestroyAllBullets(_em);

			Assert.IsTrue(_em.Exists(other), "Non-bullet entity must survive DestroyAllBullets");
		}

		// ─── ApplyPose ────────────────────────────────────────────────────────

		[Test]
		public void ApplyPose_SetsWorldPoseAndPrevWorldPose()
		{
			Entity entity = _em.CreateEntity(
				ComponentType.ReadWrite<WorldPose>(),
				ComponentType.ReadWrite<PrevWorldPose>(),
				ComponentType.ReadWrite<LocalTransform>()
			);

			Pose2D pose = new Pose2D
			{
				Position = new float2(3f, 7f),
				ThetaRad = 1.5f,
			};

			GameplaySimulationOperations.ApplyPose(_em, entity, pose);

			Pose2D worldPose = _em.GetComponentData<WorldPose>(entity).Value;
			Pose2D prevPose  = _em.GetComponentData<PrevWorldPose>(entity).Value;

			Assert.AreEqual(pose.Position, worldPose.Position);
			Assert.AreEqual(pose.ThetaRad, worldPose.ThetaRad, 0.0001f);
			Assert.AreEqual(pose.Position, prevPose.Position);
			Assert.AreEqual(pose.ThetaRad, prevPose.ThetaRad, 0.0001f);
		}

		[Test]
		public void ApplyPose_SetsLocalTransformPosition()
		{
			Entity entity = _em.CreateEntity(
				ComponentType.ReadWrite<WorldPose>(),
				ComponentType.ReadWrite<PrevWorldPose>(),
				ComponentType.ReadWrite<LocalTransform>()
			);

			Pose2D pose = new Pose2D
			{
				Position = new float2(-4f, 2f),
				ThetaRad = 0f,
			};

			GameplaySimulationOperations.ApplyPose(_em, entity, pose);

			LocalTransform localTransform = _em.GetComponentData<LocalTransform>(entity);

			Assert.AreEqual(pose.Position.x, localTransform.Position.x, 0.0001f);
			Assert.AreEqual(pose.Position.y, localTransform.Position.y, 0.0001f);
			Assert.AreEqual(0f,              localTransform.Position.z, 0.0001f);
		}

		// ─── CaptureShips ─────────────────────────────────────────────────────

		[Test]
		public void CaptureShips_WithNoShips_ReturnsEmptyArray()
		{
			ShipSaveData[] result = GameplaySimulationOperations.CaptureShips(_em);

			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.Length);
		}

		[Test]
		public void CaptureShips_CapturesShipData()
		{
			Entity entity = CreateMinimalShipEntity(stableId: 42, team: 1,
				positionX: 5f, positionY: -3f, thetaRad: 0.5f,
				health: 80, shipType: ShipType.Fighter);

			ShipSaveData[] result = GameplaySimulationOperations.CaptureShips(_em);

			Assert.AreEqual(1, result.Length);
			Assert.AreEqual(42,            result[0].StableId);
			Assert.AreEqual(1,             result[0].Team);
			Assert.AreEqual(5f,            result[0].PositionX, 0.0001f);
			Assert.AreEqual(-3f,           result[0].PositionY, 0.0001f);
			Assert.AreEqual(0.5f,          result[0].ThetaRad,  0.0001f);
			Assert.AreEqual(80,            result[0].Health);
			Assert.AreEqual((int)ShipType.Fighter, result[0].ShipType);
		}

		// ─── CaptureBullets ───────────────────────────────────────────────────

		[Test]
		public void CaptureBullets_WithNoBullets_ReturnsEmptyArray()
		{
			BulletSaveData[] result = GameplaySimulationOperations.CaptureBullets(_em);

			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.Length);
		}

		// ─── Helpers ──────────────────────────────────────────────────────────

		private Entity CreateMinimalShipEntity(
			int stableId, byte team,
			float positionX, float positionY, float thetaRad,
			short health, ShipType shipType)
		{
			Entity entity = _em.CreateEntity(
				ComponentType.ReadWrite<ShipTag>(),
				ComponentType.ReadWrite<StableId>(),
				ComponentType.ReadWrite<ShipTypeId>(),
				ComponentType.ReadWrite<TeamId>(),
				ComponentType.ReadWrite<WorldPose>(),
				ComponentType.ReadWrite<Velocity>(),
				ComponentType.ReadWrite<AngularVelocity>(),
				ComponentType.ReadWrite<Health>()
			);

			_em.SetComponentData(entity, new StableId   { Value = stableId });
			_em.SetComponentData(entity, new ShipTypeId { Value = shipType });
			_em.SetComponentData(entity, new TeamId     { Value = team });
			_em.SetComponentData(entity, new WorldPose
			{
				Value = new Pose2D
				{
					Position = new float2(positionX, positionY),
					ThetaRad = thetaRad,
				}
			});
			_em.SetComponentData(entity, new Health { Value = health });

			return entity;
		}
	}
}
