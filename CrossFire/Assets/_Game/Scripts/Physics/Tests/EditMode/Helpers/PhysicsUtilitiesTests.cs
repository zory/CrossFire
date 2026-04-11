using NUnit.Framework;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Unit tests for the pure-math methods on <see cref="PhysicsUtilities"/>.
	///
	/// These are all stateless helpers — no ECS World or entities required.
	/// Each group targets one method; edge cases (degenerate input, boundary
	/// conditions) are tested alongside the happy path.
	/// </summary>
	public class PhysicsUtilitiesTests
	{
		private const float DELTA = 1e-5f;

		// ══════════════════════════════════════════════════════════════════════════
		// Rotate
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		[Test]
		public void Rotate_ZeroRadians_ReturnsSameVector()
		{
			float2 result = PhysicsUtilities.Rotate(new float2(3f, 4f), 0f);

			Assert.AreEqual(3f, result.x, DELTA);
			Assert.AreEqual(4f, result.y, DELTA);
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// (1,0) rotated 90° CCW around the origin must land on (0,1).
		[Test]
		public void Rotate_HalfPi_RotatesCounterClockwise()
		{
			float2 result = PhysicsUtilities.Rotate(new float2(1f, 0f), math.PI / 2f);

			Assert.AreEqual(0f, result.x, DELTA);
			Assert.AreEqual(1f, result.y, DELTA);
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// (1,0) rotated 180° must flip to (−1,0).
		[Test]
		public void Rotate_Pi_FlipsVector()
		{
			float2 result = PhysicsUtilities.Rotate(new float2(1f, 0f), math.PI);

			Assert.AreEqual(-1f, result.x, DELTA);
			Assert.AreEqual(0f, result.y, DELTA);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Forward
		// Ships use a Y-up convention: at 0 radians the ship faces (0,1).
		// Positive theta is CCW, so π/2 rotates the forward vector to (−1,0).
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		[Test]
		public void Forward_ZeroRadians_PointsUp()
		{
			float2 result = PhysicsUtilities.Forward(0f);

			Assert.AreEqual(0f, result.x, DELTA);
			Assert.AreEqual(1f, result.y, DELTA);
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		[Test]
		public void Forward_HalfPi_PointsLeft()
		{
			float2 result = PhysicsUtilities.Forward(math.PI / 2f);

			Assert.AreEqual(-1f, result.x, DELTA);
			Assert.AreEqual(0f, result.y, DELTA);
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		[Test]
		public void Forward_Pi_PointsDown()
		{
			float2 result = PhysicsUtilities.Forward(math.PI);

			Assert.AreEqual(0f, result.x, DELTA);
			Assert.AreEqual(-1f, result.y, DELTA);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// TransformPoint
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		[Test]
		public void TransformPoint_NoRotationNoTranslation_ReturnsSamePoint()
		{
			float2 result = PhysicsUtilities.TransformPoint(new float2(3f, 5f), float2.zero, 0f);

			Assert.AreEqual(3f, result.x, DELTA);
			Assert.AreEqual(5f, result.y, DELTA);
		}

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		[Test]
		public void TransformPoint_TranslationOnly_AddsWorldOffset()
		{
			float2 result = PhysicsUtilities.TransformPoint(
				new float2(1f, 0f), new float2(5f, 3f), 0f);

			Assert.AreEqual(6f, result.x, DELTA);
			Assert.AreEqual(3f, result.y, DELTA);
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// (1,0) rotated 90° CCW → (0,1), then translated by (2,3) → (2,4).
		[Test]
		public void TransformPoint_RotationAndTranslation_RotationAppliedBeforeTranslation()
		{
			float2 result = PhysicsUtilities.TransformPoint(
				new float2(1f, 0f), new float2(2f, 3f), math.PI / 2f);

			Assert.AreEqual(2f, result.x, DELTA);
			Assert.AreEqual(4f, result.y, DELTA);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// DistanceSquaredPointToSegment
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 10 ──────────────────────────────────────────────────────────────
		[Test]
		public void DistanceSquaredPointToSegment_PointOnMidpoint_ReturnsZero()
		{
			float result = PhysicsUtilities.DistanceSquaredPointToSegment(
				new float2(1f, 0f), new float2(0f, 0f), new float2(2f, 0f));

			Assert.AreEqual(0f, result, DELTA);
		}

		// ─── Test 11 ──────────────────────────────────────────────────────────────
		// Segment (0,0)–(2,0), point (1,3): closest point on segment is (1,0), dist² = 9.
		[Test]
		public void DistanceSquaredPointToSegment_PointPerpendicularToMidpoint_ReturnsHeightSquared()
		{
			float result = PhysicsUtilities.DistanceSquaredPointToSegment(
				new float2(1f, 3f), new float2(0f, 0f), new float2(2f, 0f));

			Assert.AreEqual(9f, result, DELTA);
		}

		// ─── Test 12 ──────────────────────────────────────────────────────────────
		// Segment (0,0)–(1,0), point (3,0): projection overshoots, clamps to (1,0), dist² = 4.
		[Test]
		public void DistanceSquaredPointToSegment_PointBeyondEnd_ReturnsDistanceToEndpointSquared()
		{
			float result = PhysicsUtilities.DistanceSquaredPointToSegment(
				new float2(3f, 0f), new float2(0f, 0f), new float2(1f, 0f));

			Assert.AreEqual(4f, result, DELTA);
		}

		// ─── Test 13 ──────────────────────────────────────────────────────────────
		// Segment (0,0)–(1,0), point (−2,0): projection undershoots, clamps to (0,0), dist² = 4.
		[Test]
		public void DistanceSquaredPointToSegment_PointBeforeStart_ReturnsDistanceToStartSquared()
		{
			float result = PhysicsUtilities.DistanceSquaredPointToSegment(
				new float2(-2f, 0f), new float2(0f, 0f), new float2(1f, 0f));

			Assert.AreEqual(4f, result, DELTA);
		}

		// ─── Test 14 ──────────────────────────────────────────────────────────────
		// Zero-length segment: the denominator guard clamps to 1e-12 so the result
		// must be the squared distance from the point to that single degenerate point.
		// Point (4,5), segment (1,1)–(1,1): dist² = 3² + 4² = 25.
		[Test]
		public void DistanceSquaredPointToSegment_DegenerateZeroLengthSegment_ReturnsStraightDistanceSquared()
		{
			float result = PhysicsUtilities.DistanceSquaredPointToSegment(
				new float2(4f, 5f), new float2(1f, 1f), new float2(1f, 1f));

			Assert.AreEqual(25f, result, DELTA);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// PointInTriangle — CCW triangle: a=(0,0), b=(2,0), c=(1,2)
		// ══════════════════════════════════════════════════════════════════════════

		private static readonly float2 _ptA = new float2(0f, 0f);
		private static readonly float2 _ptB = new float2(2f, 0f);
		private static readonly float2 _ptC = new float2(1f, 2f);

		// ─── Test 15 ──────────────────────────────────────────────────────────────
		[Test]
		public void PointInTriangle_PointAtCentroid_ReturnsTrue()
		{
			float2 centroid = (_ptA + _ptB + _ptC) / 3f;

			Assert.IsTrue(PhysicsUtilities.PointInTriangle(centroid, _ptA, _ptB, _ptC));
		}

		// ─── Test 16 ──────────────────────────────────────────────────────────────
		[Test]
		public void PointInTriangle_PointClearlyOutside_ReturnsFalse()
		{
			Assert.IsFalse(PhysicsUtilities.PointInTriangle(new float2(5f, 5f), _ptA, _ptB, _ptC));
		}

		// ─── Test 17 ──────────────────────────────────────────────────────────────
		// A point exactly on an edge produces one zero cross product; the remaining
		// two have the same sign, so the method must return true (inside/on boundary).
		[Test]
		public void PointInTriangle_PointOnEdgeMidpoint_ReturnsTrue()
		{
			float2 midAB = (_ptA + _ptB) * 0.5f; // (1, 0)

			Assert.IsTrue(PhysicsUtilities.PointInTriangle(midAB, _ptA, _ptB, _ptC));
		}

		// ─── Test 18 ──────────────────────────────────────────────────────────────
		[Test]
		public void PointInTriangle_PointAtVertex_ReturnsTrue()
		{
			Assert.IsTrue(PhysicsUtilities.PointInTriangle(_ptA, _ptA, _ptB, _ptC));
		}

		// ─── Test 19 ──────────────────────────────────────────────────────────────
		[Test]
		public void PointInTriangle_PointOutsideBelowBase_ReturnsFalse()
		{
			Assert.IsFalse(PhysicsUtilities.PointInTriangle(new float2(1f, -1f), _ptA, _ptB, _ptC));
		}

		// ══════════════════════════════════════════════════════════════════════════
		// CanCollide — bidirectional layer/mask filter
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 20 ──────────────────────────────────────────────────────────────
		// Both entities must be able to see each other's layer for a collision to register.
		[Test]
		public void CanCollide_BidirectionalMasks_ReturnsTrue()
		{
			Assert.IsTrue(PhysicsUtilities.CanCollide(
				new CollisionLayer { Value = 1u }, new CollisionMask { Value = 2u },
				new CollisionLayer { Value = 2u }, new CollisionMask { Value = 1u }));
		}

		// ─── Test 21 ──────────────────────────────────────────────────────────────
		// If the first entity's mask does not include the second entity's layer,
		// the pair must not collide regardless of the second entity's mask.
		[Test]
		public void CanCollide_FirstMaskDoesNotSeeSecondLayer_ReturnsFalse()
		{
			// A (mask=4) cannot see B (layer=2); B (mask=1) can see A (layer=1).
			Assert.IsFalse(PhysicsUtilities.CanCollide(
				new CollisionLayer { Value = 1u }, new CollisionMask { Value = 4u },
				new CollisionLayer { Value = 2u }, new CollisionMask { Value = 1u }));
		}

		// ─── Test 22 ──────────────────────────────────────────────────────────────
		// If the second entity's mask does not include the first entity's layer,
		// the pair must not collide regardless of the first entity's mask.
		[Test]
		public void CanCollide_SecondMaskDoesNotSeeFirstLayer_ReturnsFalse()
		{
			// A (mask=2) can see B (layer=2); B (mask=4) cannot see A (layer=1).
			Assert.IsFalse(PhysicsUtilities.CanCollide(
				new CollisionLayer { Value = 1u }, new CollisionMask { Value = 2u },
				new CollisionLayer { Value = 2u }, new CollisionMask { Value = 4u }));
		}

		// ─── Test 23 ──────────────────────────────────────────────────────────────
		[Test]
		public void CanCollide_BothMasksZero_ReturnsFalse()
		{
			Assert.IsFalse(PhysicsUtilities.CanCollide(
				new CollisionLayer { Value = 1u }, new CollisionMask { Value = 0u },
				new CollisionLayer { Value = 2u }, new CollisionMask { Value = 0u }));
		}

		// ─── Test 24 ──────────────────────────────────────────────────────────────
		[Test]
		public void CanCollide_BothMasksMatchAllLayers_ReturnsTrue()
		{
			Assert.IsTrue(PhysicsUtilities.CanCollide(
				new CollisionLayer { Value = 1u }, new CollisionMask { Value = 0xFFFFFFFFu },
				new CollisionLayer { Value = 2u }, new CollisionMask { Value = 0xFFFFFFFFu }));
		}
	}
}
