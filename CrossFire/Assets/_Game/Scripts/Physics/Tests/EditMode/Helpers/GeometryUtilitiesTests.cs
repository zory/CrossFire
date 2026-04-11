using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Unit tests for the intersection methods on <see cref="GeometryUtilities"/>.
	///
	/// The three intersection paths tested are:
	///   Circle vs Circle            — <see cref="GeometryUtilities.CircleIntersectsCircle"/>
	///   Circle vs Triangle          — <see cref="GeometryUtilities.CircleIntersectsTriangleWorld"/>
	///   Circle vs Triangle soup     — <see cref="GeometryUtilities.CircleIntersectsTriangleSoupWorld"/>
	///
	/// The drawing helpers (ForEachCircleSegment, etc.) are not tested here because they
	/// delegate entirely to a caller-supplied callback with no logic of their own.
	///
	/// Note: <see cref="GeometryUtilities.CircleIntersectsTriangleWorld"/> takes
	/// <c>circleRadiusSquared</c>, not radius.
	/// </summary>
	public class GeometryUtilitiesTests
	{
		private const float DELTA = 1e-5f;

		// ══════════════════════════════════════════════════════════════════════════
		// CircleIntersectsCircle
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// distance=1, combined radius=2 → overlapping.
		[Test]
		public void CircleIntersectsCircle_ClearlyOverlapping_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsCircle(
				new float2(0f, 0f), 1f,
				new float2(1f, 0f), 1f));
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// distance=2, combined radius=2 → exactly touching.
		// The check is ≤ so touching must return true.
		[Test]
		public void CircleIntersectsCircle_ExactlyTouching_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsCircle(
				new float2(0f, 0f), 1f,
				new float2(2f, 0f), 1f));
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// distance=10, combined radius=2 → clearly separated.
		[Test]
		public void CircleIntersectsCircle_ClearlySeparated_ReturnsFalse()
		{
			Assert.IsFalse(GeometryUtilities.CircleIntersectsCircle(
				new float2(0f, 0f), 1f,
				new float2(10f, 0f), 1f));
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Two concentric circles always intersect.
		[Test]
		public void CircleIntersectsCircle_SameCenter_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsCircle(
				new float2(3f, 3f), 1f,
				new float2(3f, 3f), 1f));
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// Two zero-radius circles at the same point: dist²=0, combinedRadius=0, 0≤0 → true.
		[Test]
		public void CircleIntersectsCircle_BothZeroRadiusSamePoint_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsCircle(
				new float2(1f, 1f), 0f,
				new float2(1f, 1f), 0f));
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// Two zero-radius circles at different points: dist²>0, combinedRadius=0 → false.
		[Test]
		public void CircleIntersectsCircle_BothZeroRadiusDifferentPoints_ReturnsFalse()
		{
			Assert.IsFalse(GeometryUtilities.CircleIntersectsCircle(
				new float2(0f, 0f), 0f,
				new float2(1f, 0f), 0f));
		}

		// ══════════════════════════════════════════════════════════════════════════
		// CircleIntersectsTriangleWorld
		// Reference triangle: a=(−1,−1), b=(1,−1), c=(0,1)
		// Note: third parameter is circleRadiusSquared, not radius.
		// ══════════════════════════════════════════════════════════════════════════

		private static readonly float2 _a = new float2(-1f, -1f);
		private static readonly float2 _b = new float2(1f, -1f);
		private static readonly float2 _c = new float2(0f, 1f);

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Circle centre (0,0) is inside the triangle → PointInTriangle returns true immediately.
		[Test]
		public void CircleIntersectsTriangleWorld_CentreInsideTriangle_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsTriangleWorld(
				new float2(0f, 0f), circleRadiusSquared: 0.01f,
				_a, _b, _c));
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// Bottom edge at y=−1; circle centre at (0,−1.05), radius=0.1 (radiusSq=0.01).
		// Distance to the bottom edge = 0.05 < 0.1 → touching.
		[Test]
		public void CircleIntersectsTriangleWorld_CircleTouchingBottomEdge_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsTriangleWorld(
				new float2(0f, -1.05f), circleRadiusSquared: 0.01f,
				_a, _b, _c));
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// Circle at (0,−5) with radius 0.1 — far below the triangle, no overlap.
		[Test]
		public void CircleIntersectsTriangleWorld_CircleClearlyOutside_ReturnsFalse()
		{
			Assert.IsFalse(GeometryUtilities.CircleIntersectsTriangleWorld(
				new float2(0f, -5f), circleRadiusSquared: 0.01f,
				_a, _b, _c));
		}

		// ─── Test 10 ──────────────────────────────────────────────────────────────
		// Circle centred on vertex a — centre is on the triangle boundary → true.
		[Test]
		public void CircleIntersectsTriangleWorld_CircleCentredOnVertex_ReturnsTrue()
		{
			Assert.IsTrue(GeometryUtilities.CircleIntersectsTriangleWorld(
				_a, circleRadiusSquared: 0.01f,
				_a, _b, _c));
		}

		// ══════════════════════════════════════════════════════════════════════════
		// CircleIntersectsTriangleSoupWorld
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 11 ──────────────────────────────────────────────────────────────
		// Circle centre inside the only triangle in the soup → hit.
		[Test]
		public void CircleIntersectsTriangleSoupWorld_CentreInsideSingleTriangle_ReturnsTrue()
		{
			BlobAssetReference<TriangleSoupBlob> blob = BuildBlob(
				new float2(-1f, -1f), new float2(1f, -1f), new float2(0f, 1f));

			bool result = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(0f, 0f),
				circleRadius: 0.1f,
				targetPositionWorld: float2.zero,
				targetRotationRad: 0f,
				trianglesLocal: ref blob.Value.Vertices);

			blob.Dispose();
			Assert.IsTrue(result);
		}

		// ─── Test 12 ──────────────────────────────────────────────────────────────
		// Circle far outside all triangles → miss.
		[Test]
		public void CircleIntersectsTriangleSoupWorld_CircleOutsideAllTriangles_ReturnsFalse()
		{
			BlobAssetReference<TriangleSoupBlob> blob = BuildBlob(
				new float2(-1f, -1f), new float2(1f, -1f), new float2(0f, 1f));

			bool result = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(0f, -5f),
				circleRadius: 0.1f,
				targetPositionWorld: float2.zero,
				targetRotationRad: 0f,
				trianglesLocal: ref blob.Value.Vertices);

			blob.Dispose();
			Assert.IsFalse(result);
		}

		// ─── Test 13 ──────────────────────────────────────────────────────────────
		// When the triangle soup entity is translated the world-space transform must
		// be applied before the intersection test.
		// Circle at (5,0) misses the triangle at origin but hits when the triangle
		// is placed at (5,0).
		[Test]
		public void CircleIntersectsTriangleSoupWorld_TranslatedTriangle_CircleAtNewPositionReturnsTrue()
		{
			BlobAssetReference<TriangleSoupBlob> blob = BuildBlob(
				new float2(-1f, -1f), new float2(1f, -1f), new float2(0f, 1f));

			bool missAtOrigin = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(5f, 0f),
				circleRadius: 0.1f,
				targetPositionWorld: float2.zero,
				targetRotationRad: 0f,
				trianglesLocal: ref blob.Value.Vertices);

			bool hitAtTranslatedPosition = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(5f, 0f),
				circleRadius: 0.1f,
				targetPositionWorld: new float2(5f, 0f),
				targetRotationRad: 0f,
				trianglesLocal: ref blob.Value.Vertices);

			blob.Dispose();
			Assert.IsFalse(missAtOrigin, "Circle at (5,0) should miss the triangle placed at origin");
			Assert.IsTrue(hitAtTranslatedPosition, "Circle at (5,0) should hit the triangle placed at (5,0)");
		}

		// ─── Test 14 ──────────────────────────────────────────────────────────────
		// Rotating the triangle soup must transform its local vertices before the test.
		// Upward-pointing triangle (-0.5,0),(0.5,0),(0,2): circle at (0,1) is inside.
		// After 90° CCW rotation the triangle points left; (0,1) is now outside.
		[Test]
		public void CircleIntersectsTriangleSoupWorld_RotatedTriangle_CircleHitsOriginalMissesRotated()
		{
			// Narrow upward triangle, circle at (0,1) is near the interior.
			BlobAssetReference<TriangleSoupBlob> blob = BuildBlob(
				new float2(-0.5f, 0f), new float2(0.5f, 0f), new float2(0f, 2f));

			bool hitUnrotated = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(0f, 1f),
				circleRadius: 0.1f,
				targetPositionWorld: float2.zero,
				targetRotationRad: 0f,
				trianglesLocal: ref blob.Value.Vertices);

			// 90° CCW: (−0.5,0)→(0,−0.5), (0.5,0)→(0,0.5), (0,2)→(−2,0) — triangle points left.
			// Circle at (0,1) is above the rotated triangle's extents and outside it.
			bool hitRotated = GeometryUtilities.CircleIntersectsTriangleSoupWorld(
				circleCenterWorld: new float2(0f, 1f),
				circleRadius: 0.1f,
				targetPositionWorld: float2.zero,
				targetRotationRad: math.PI / 2f,
				trianglesLocal: ref blob.Value.Vertices);

			blob.Dispose();
			Assert.IsTrue(hitUnrotated, "Circle at (0,1) should be inside the un-rotated upward triangle");
			Assert.IsFalse(hitRotated, "Circle at (0,1) should be outside the 90°-rotated (leftward) triangle");
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		private static BlobAssetReference<TriangleSoupBlob> BuildBlob(float2 a, float2 b, float2 c)
		{
			BlobBuilder builder = new BlobBuilder(Allocator.Temp);
			ref TriangleSoupBlob root = ref builder.ConstructRoot<TriangleSoupBlob>();
			BlobBuilderArray<float2> verts = builder.Allocate(ref root.Vertices, 3);
			verts[0] = a;
			verts[1] = b;
			verts[2] = c;
			BlobAssetReference<TriangleSoupBlob> blobRef =
				builder.CreateBlobAssetReference<TriangleSoupBlob>(Allocator.Persistent);
			builder.Dispose();
			return blobRef;
		}
	}
}
