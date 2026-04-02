using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public class Vector3IntComparer : IComparer<Vector3Int>
	{
		public int Compare(Vector3Int a, Vector3Int b)
		{
			if (a.z != b.z)
			{
				return a.z.CompareTo(b.z);
			}

			if (a.x != b.x)
			{
				return a.x.CompareTo(b.x);
			}

			return a.y.CompareTo(b.y);
		}
	}

	public static class HexHelpers
    {
		private const float HALF_SQRT3 = 0.8660254037844386f; // sqrt(3) / 2

		// Distance from center to the middle of an edge (inradius).
		public static float GetPointyHexApothem(float radius)
		{
			return HALF_SQRT3 * radius;
		}

		// World-space midpoint of edge i on the XZ plane. Edge i connects vertex i and vertex i+1.
		public static Vector3 GetPointyHexEdgeMidpointXZ(int edgeIndex, float radius)
		{
			edgeIndex = Mod(edgeIndex, 6);

			Vector3 v0 = GetVertexXZ(edgeIndex,     radius);
			Vector3 v1 = GetVertexXZ(edgeIndex + 1, radius);

			return (v0 + v1) * 0.5f;
		}

		// Y rotation in degrees so an object lies flat along edge i (local +X along the edge).
		public static float GetPointyHexEdgeRotationDeg(int index)
		{
			index = Mod(index, 6);

			return index switch
			{
				0 => 30f,
				1 => 90f,
				2 => 150f,
				3 => 210f,
				4 => 270f,
				5 => 330f,
				_ => 0f
			};
		}

		// Vertex position on the XZ plane. Index 0 = top, then clockwise.
		private static Vector3 GetVertexXZ(int index, float radius)
		{
			index = Mod(index, 6);

			float angleDeg = 90f - index * 60f;
			float angleRad = angleDeg * Mathf.Deg2Rad;

			return new Vector3(radius * Mathf.Cos(angleRad), 0f, radius * Mathf.Sin(angleRad));
		}

		private static int Mod(int value, int modulo)
		{
			int r = value % modulo;
			return r < 0 ? r + modulo : r;
		}
	}
}
