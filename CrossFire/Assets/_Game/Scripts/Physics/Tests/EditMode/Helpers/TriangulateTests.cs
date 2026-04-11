using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Unit tests for <see cref="PhysicsUtilities.Triangulate"/> and its closely related
	/// helpers <see cref="PhysicsUtilities.SignedArea"/> and <see cref="PhysicsUtilities.IsConvex"/>.
	///
	/// Triangulate uses ear-clipping and is the most complex method in the helpers layer.
	/// Bugs here silently produce wrong collider geometry (baked at editor time) that tests
	/// at the system level would not catch.
	/// </summary>
	public class TriangulateTests
	{
		private const float DELTA = 1e-5f;

		// ══════════════════════════════════════════════════════════════════════════
		// SignedArea
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// CCW winding produces a positive signed area.
		[Test]
		public void SignedArea_CCWTriangle_ReturnsPositive()
		{
			float2[] verts = { new float2(0f, 0f), new float2(1f, 0f), new float2(0f, 1f) };

			float area = PhysicsUtilities.SignedArea(verts);

			Assert.Greater(area, 0f);
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// CW winding produces a negative signed area.
		[Test]
		public void SignedArea_CWTriangle_ReturnsNegative()
		{
			float2[] verts = { new float2(0f, 0f), new float2(0f, 1f), new float2(1f, 0f) };

			float area = PhysicsUtilities.SignedArea(verts);

			Assert.Less(area, 0f);
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// The magnitude of the signed area of a unit right triangle must be 0.5.
		[Test]
		public void SignedArea_UnitRightTriangle_AbsoluteValueIsHalf()
		{
			float2[] verts = { new float2(0f, 0f), new float2(1f, 0f), new float2(0f, 1f) };

			float area = PhysicsUtilities.SignedArea(verts);

			Assert.AreEqual(0.5f, math.abs(area), DELTA);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// IsConvex
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// A left (CCW) turn at vertex b is convex in a CCW polygon.
		[Test]
		public void IsConvex_LeftTurn_ReturnsTrue()
		{
			// a→b→c makes a 90° left turn
			Assert.IsTrue(PhysicsUtilities.IsConvex(
				new float2(0f, 0f),
				new float2(1f, 0f),
				new float2(1f, 1f)));
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// A right (CW) turn at vertex b is reflex (not convex) in a CCW polygon.
		[Test]
		public void IsConvex_RightTurn_ReturnsFalse()
		{
			// a→b→c makes a 90° right turn
			Assert.IsFalse(PhysicsUtilities.IsConvex(
				new float2(0f, 0f),
				new float2(1f, 1f),
				new float2(2f, 0f)));
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Triangulate — degenerate / edge cases
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		[Test]
		public void Triangulate_NullInput_ReturnsEmptyList()
		{
			List<float2> result = PhysicsUtilities.Triangulate(null);

			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.Count);
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		[Test]
		public void Triangulate_FewerThanThreeVertices_ReturnsEmptyList()
		{
			float2[] twoVerts = { new float2(0f, 0f), new float2(1f, 0f) };

			List<float2> result = PhysicsUtilities.Triangulate(twoVerts);

			Assert.AreEqual(0, result.Count);
		}

		// ══════════════════════════════════════════════════════════════════════════
		// Triangulate — simple polygon cases
		// ══════════════════════════════════════════════════════════════════════════

		// ─── Test 8 ───────────────────────────────────────────────────────────────
		// A CCW triangle input must yield exactly one output triangle (3 vertices).
		[Test]
		public void Triangulate_ThreeVertexCCWTriangle_ReturnsSingleTriangle()
		{
			float2[] outline = { new float2(0f, 0f), new float2(1f, 0f), new float2(0f, 1f) };

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			Assert.AreEqual(3, result.Count, "Three-vertex polygon must produce one triangle");
		}

		// ─── Test 9 ───────────────────────────────────────────────────────────────
		// A CW triangle triggers the winding-reversal branch and must still
		// produce a valid single output triangle.
		[Test]
		public void Triangulate_ThreeVertexCWTriangle_ReturnsSingleTriangle()
		{
			// Reversed winding of Test 8
			float2[] outline = { new float2(0f, 0f), new float2(0f, 1f), new float2(1f, 0f) };

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			Assert.AreEqual(3, result.Count, "CW input must be normalised to CCW and still yield one triangle");
		}

		// ─── Test 10 ──────────────────────────────────────────────────────────────
		// A convex CCW quad must produce 2 triangles (6 vertices).
		// Unit square: (0,0)→(1,0)→(1,1)→(0,1)
		[Test]
		public void Triangulate_ConvexQuad_ReturnsTwoTriangles()
		{
			float2[] outline =
			{
				new float2(0f, 0f),
				new float2(1f, 0f),
				new float2(1f, 1f),
				new float2(0f, 1f)
			};

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			Assert.AreEqual(6, result.Count, "Quad must produce 2 triangles = 6 vertices");
		}

		// ─── Test 11 ──────────────────────────────────────────────────────────────
		// A CCW L-shaped polygon (6 vertices, one reflex corner) must produce
		// 4 triangles (12 vertices).
		//
		//   (0,2)──(1,2)
		//   |      |
		//   |   (1,1)──(2,1)
		//   |           |
		//   (0,0)──────(2,0)
		[Test]
		public void Triangulate_ConcaveLShape_ReturnsFourTriangles()
		{
			float2[] outline =
			{
				new float2(0f, 0f),
				new float2(2f, 0f),
				new float2(2f, 1f),
				new float2(1f, 1f),
				new float2(1f, 2f),
				new float2(0f, 2f)
			};

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			Assert.AreEqual(12, result.Count, "6-vertex L-shape must produce 4 triangles = 12 vertices");
		}

		// ─── Test 12 ──────────────────────────────────────────────────────────────
		// Output vertex count must always be a multiple of 3 (each entry is a triangle vertex).
		[Test]
		public void Triangulate_OutputCountIsDivisibleByThree()
		{
			float2[] outline =
			{
				new float2(0f, 0f),
				new float2(3f, 0f),
				new float2(3f, 1f),
				new float2(2f, 1f),
				new float2(2f, 2f),
				new float2(1f, 2f),
				new float2(1f, 1f),
				new float2(0f, 1f)
			};

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			Assert.AreEqual(0, result.Count % 3, "Output vertex count must always be divisible by 3");
		}

		// ─── Test 13 ──────────────────────────────────────────────────────────────
		// Every vertex in the triangulation output must be one of the original
		// outline vertices. Ear-clipping never invents new points.
		[Test]
		public void Triangulate_AllOutputVerticesMatchInputOutline()
		{
			float2[] outline =
			{
				new float2(0f, 0f),
				new float2(2f, 0f),
				new float2(2f, 1f),
				new float2(1f, 1f),
				new float2(1f, 2f),
				new float2(0f, 2f)
			};

			List<float2> result = PhysicsUtilities.Triangulate(outline);

			foreach (float2 outputVertex in result)
			{
				bool foundInOutline = false;

				for (int i = 0; i < outline.Length; i++)
				{
					if (math.abs(outline[i].x - outputVertex.x) <= DELTA &&
						math.abs(outline[i].y - outputVertex.y) <= DELTA)
					{
						foundInOutline = true;
						break;
					}
				}

				Assert.IsTrue(foundInOutline,
					$"Output vertex ({outputVertex.x},{outputVertex.y}) is not present in the input outline");
			}
		}
	}
}
