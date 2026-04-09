using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for CollisionDetectionSystem.
	/// The system performs broadphase grid partitioning followed by narrowphase intersection
	/// tests, writing results to a singleton CollisionEvent buffer that is cleared at the
	/// start of every update.
	///
	/// Supported narrowphase pairs:
	///   - Circle    vs Circle          — fully implemented
	///   - Circle    vs ConcaveTriangles — fully implemented (both orderings)
	///   - ConcaveTriangles vs ConcaveTriangles — NOT implemented (always returns false)
	///
	/// The broadphase cell size is set to 10 world units so nearby test entities always
	/// share grid cells.
	/// </summary>
	public class CollisionDetectionSystemTests : PhysicsTestBase
	{
		// Collision layer/mask constants used across tests.
		private const uint LAYER_A = 1u;
		private const uint LAYER_B = 2u;
		private const uint MASK_ALL = 0xFFFFFFFFu; // collides with every layer
		private const uint MASK_NONE = 0u;         // collides with nothing

		// Triangle used in all triangle-collider tests:
		// vertices at (-1,-1), (1,-1), (0,1) — a triangle centred near the origin.
		private static readonly float2 TRI_A = new float2(-1f, -1f);
		private static readonly float2 TRI_B = new float2(1f, -1f);
		private static readonly float2 TRI_C = new float2(0f, 1f);
		private const float TRI_BOUND_RADIUS = 2f;

		private Entity _bufferEntity;
		private readonly List<BlobAssetReference<TriangleSoupBlob>> _blobs =
			new List<BlobAssetReference<TriangleSoupBlob>>();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<CollisionDetectionSystem>();
			_bufferEntity = PhysicsEntityFactory.CreateCollisionSingletons(_entityManager, cellSize: 10f);
		}

		[TearDown]
		public override void TearDown()
		{
			foreach (BlobAssetReference<TriangleSoupBlob> blob in _blobs)
			{
				blob.Dispose();
			}
			_blobs.Clear();
			base.TearDown();
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		private DynamicBuffer<CollisionEvent> GetCollisionEvents()
		{
			return _entityManager.GetBuffer<CollisionEvent>(_bufferEntity);
		}

		/// <summary>
		/// Returns true if the buffer contains an event for the given entity pair,
		/// regardless of which entity is FirstEntity and which is SecondEntity.
		/// </summary>
		private bool ContainsPair(DynamicBuffer<CollisionEvent> events, Entity a, Entity b)
		{
			for (int i = 0; i < events.Length; i++)
			{
				bool match = (events[i].FirstEntity == a && events[i].SecondEntity == b)
						  || (events[i].FirstEntity == b && events[i].SecondEntity == a);
				if (match)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Creates a single-triangle blob and registers it for disposal in TearDown.
		/// </summary>
		private BlobAssetReference<TriangleSoupBlob> MakeTriangleBlob(float2 a, float2 b, float2 c)
		{
			BlobAssetReference<TriangleSoupBlob> blob =
				PhysicsEntityFactory.CreateSingleTriangleBlob(a, b, c);
			_blobs.Add(blob);
			return blob;
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Circle vs Circle
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// When no collider entities exist the system must still run (to clear the buffer)
		// and produce zero events.

		[Test]
		public void OnUpdate_NoColliders_CollisionBufferIsEmpty()
		{
			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length, "No colliders should produce no events");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// Two overlapping circles must produce exactly one collision event referencing both.

		[Test]
		public void CircleVsCircle_Overlapping_OneEventGenerated()
		{
			// distance = 1, combined radius = 2 → overlapping
			Entity entityA = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			Entity entityB = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(1f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			DynamicBuffer<CollisionEvent> events = GetCollisionEvents();
			Assert.AreEqual(1, events.Length, "Exactly one event expected for one overlapping pair");
			Assert.IsTrue(ContainsPair(events, entityA, entityB), "Event must reference both entities");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Two circles far enough apart must produce no event.

		[Test]
		public void CircleVsCircle_NonOverlapping_NoEventGenerated()
		{
			// distance = 100, combined radius = 2 → not overlapping
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(100f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length, "Distant circles must not collide");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// When the broadphase passes (large BoundRadius) but the narrowphase fails
		// (small CircleRadius), no event must be emitted.

		[Test]
		public void CircleVsCircle_BroadphasePassNarrowphaseFail_NoEventGenerated()
		{
			// distance = 4; BoundRadius = 5 → combined bound = 10 → broadphase passes
			// distance = 4; CircleRadius = 0.5 → combined circle = 1 → narrowphase fails
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f),
				circleRadius: 0.5f, boundRadius: 5f, layer: LAYER_A, mask: MASK_ALL);
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(4f, 0f),
				circleRadius: 0.5f, boundRadius: 5f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length,
				"Broadphase hit but narrowphase miss must produce no event");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Each overlapping pair must be reported exactly once — the deduplication set
		// prevents double-reporting when both entities scan each other's grid cells.

		[Test]
		public void CircleVsCircle_Overlapping_PairEmittedExactlyOnce()
		{
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(1f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(1, GetCollisionEvents().Length,
				"Each pair must be reported exactly once, not once per entity");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Three mutually overlapping circles must produce three events — one per unique pair.

		[Test]
		public void CircleVsCircle_ThreeMutuallyOverlapping_AllThreePairsEmitted()
		{
			Entity entityA = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			Entity entityB = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0.5f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			Entity entityC = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(-0.5f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			DynamicBuffer<CollisionEvent> events = GetCollisionEvents();
			Assert.AreEqual(3, events.Length, "Three overlapping circles must produce three unique pair events");
			Assert.IsTrue(ContainsPair(events, entityA, entityB), "A-B pair missing");
			Assert.IsTrue(ContainsPair(events, entityA, entityC), "A-C pair missing");
			Assert.IsTrue(ContainsPair(events, entityB, entityC), "B-C pair missing");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// A single collider entity must never generate a self-collision event.

		[Test]
		public void CircleVsCircle_SingleEntity_NoSelfCollision()
		{
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length, "A single entity must not collide with itself");
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Collision filter
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// When A's mask covers B's layer but B's mask does not cover A's layer,
		// CanCollide returns false and no event is generated.

		[Test]
		public void CollisionFilter_OneSidedMask_NoEventGenerated()
		{
			// A (layer=1, mask=2) can see B (layer=2), but B (layer=2, mask=4) cannot see A (layer=1).
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f,
				layer: LAYER_A, mask: LAYER_B);
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(1f, 0f), circleRadius: 1f,
				layer: LAYER_B, mask: 4u);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length, "One-sided mask must prevent collision event");
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// When both masks are bidirectional, overlapping circles produce an event.

		[Test]
		public void CollisionFilter_BidirectionalMask_EventGenerated()
		{
			// A (layer=1, mask=2) sees B (layer=2). B (layer=2, mask=1) sees A (layer=1).
			Entity entityA = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f,
				layer: LAYER_A, mask: LAYER_B);
			Entity entityB = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(1f, 0f), circleRadius: 1f,
				layer: LAYER_B, mask: LAYER_A);

			_world.Update();

			DynamicBuffer<CollisionEvent> events = GetCollisionEvents();
			Assert.AreEqual(1, events.Length, "Bidirectional mask must produce one event");
			Assert.IsTrue(ContainsPair(events, entityA, entityB));
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Buffer lifecycle
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 10 ──────────────────────────────────────────────────────────────
		// Events from frame N must not persist into frame N+1 when entities no longer overlap.

		[Test]
		public void Buffer_ClearedEachUpdate_StaleEventsNotRetained()
		{
			Entity entityA = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);
			Entity entityB = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(1f, 0f), circleRadius: 1f, layer: LAYER_A, mask: MASK_ALL);

			// First update: circles overlap → 1 event.
			_world.Update();
			Assert.AreEqual(1, GetCollisionEvents().Length, "First update must produce 1 event");

			// Separate the circles so they no longer overlap.
			_entityManager.SetComponentData(entityB, new WorldPose
			{
				Value = new Pose2D { Position = new float2(100f, 0f), ThetaRad = 0f }
			});

			// Second update: circles are apart → buffer must be cleared and stay empty.
			_world.Update();
			Assert.AreEqual(0, GetCollisionEvents().Length,
				"Stale events from the previous frame must not be retained");
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Circle vs ConcaveTriangles
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 11 ──────────────────────────────────────────────────────────────
		// Circle whose centre lies inside the triangle must produce a collision event.

		[Test]
		public void CircleVsTriangle_CircleCentreInsideTriangle_EventGenerated()
		{
			BlobAssetReference<TriangleSoupBlob> blob = MakeTriangleBlob(TRI_A, TRI_B, TRI_C);

			Entity circle = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, 0f), circleRadius: 0.1f, layer: LAYER_A, mask: MASK_ALL);
			Entity triangle = PhysicsEntityFactory.CreateTriangleColliderEntity(
				_entityManager, new float2(0f, 0f), TRI_BOUND_RADIUS, blob, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			DynamicBuffer<CollisionEvent> events = GetCollisionEvents();
			Assert.AreEqual(1, events.Length, "Circle inside triangle must produce one event");
			Assert.IsTrue(ContainsPair(events, circle, triangle));
		}

		// ─── Test 12 ──────────────────────────────────────────────────────────────
		// Circle touching a triangle edge (but centre outside the triangle) must also
		// produce a collision event.

		[Test]
		public void CircleVsTriangle_CircleTouchingEdge_EventGenerated()
		{
			BlobAssetReference<TriangleSoupBlob> blob = MakeTriangleBlob(TRI_A, TRI_B, TRI_C);

			// The bottom edge of the triangle runs from (-1,-1) to (1,-1) at y=-1.
			// Circle centre at (0,-1.05) with radius 0.1: distance to edge = 0.05 < radius → touching.
			Entity circle = PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, -1.05f), circleRadius: 0.1f, layer: LAYER_A, mask: MASK_ALL);
			Entity triangle = PhysicsEntityFactory.CreateTriangleColliderEntity(
				_entityManager, new float2(0f, 0f), TRI_BOUND_RADIUS, blob, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			DynamicBuffer<CollisionEvent> events = GetCollisionEvents();
			Assert.AreEqual(1, events.Length, "Circle touching triangle edge must produce one event");
			Assert.IsTrue(ContainsPair(events, circle, triangle));
		}

		// ─── Test 13 ──────────────────────────────────────────────────────────────
		// Circle completely outside and not touching the triangle must produce no event.

		[Test]
		public void CircleVsTriangle_CircleOutside_NoEventGenerated()
		{
			BlobAssetReference<TriangleSoupBlob> blob = MakeTriangleBlob(TRI_A, TRI_B, TRI_C);

			// Circle far below the triangle — no overlap with any edge or interior.
			PhysicsEntityFactory.CreateCircleColliderEntity(
				_entityManager, new float2(0f, -5f), circleRadius: 0.5f, layer: LAYER_A, mask: MASK_ALL);
			PhysicsEntityFactory.CreateTriangleColliderEntity(
				_entityManager, new float2(0f, 0f), TRI_BOUND_RADIUS, blob, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length,
				"Circle outside triangle bounds must produce no event");
		}

		// ─── Test 14 ──────────────────────────────────────────────────────────────
		// The triangle-vs-triangle narrowphase is not implemented and must always return
		// false. Even two perfectly overlapping triangle colliders must produce no event.

		[Test]
		public void TriangleVsTriangle_NotImplemented_NoEventGenerated()
		{
			BlobAssetReference<TriangleSoupBlob> blobA = MakeTriangleBlob(TRI_A, TRI_B, TRI_C);
			BlobAssetReference<TriangleSoupBlob> blobB = MakeTriangleBlob(TRI_A, TRI_B, TRI_C);

			// Both triangles at the same position so the broadphase definitely passes.
			PhysicsEntityFactory.CreateTriangleColliderEntity(
				_entityManager, new float2(0f, 0f), TRI_BOUND_RADIUS, blobA, layer: LAYER_A, mask: MASK_ALL);
			PhysicsEntityFactory.CreateTriangleColliderEntity(
				_entityManager, new float2(0f, 0f), TRI_BOUND_RADIUS, blobB, layer: LAYER_A, mask: MASK_ALL);

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length,
				"Triangle vs triangle narrowphase is not implemented and must produce no event");
		}
	}
}
