using UnityEngine;

namespace CrossFire.HexMap
{
    public static class HexHelpers
    {
		private const float Sqrt3 = 1.7320508075688772f;
		private const float HalfSqrt3 = 0.8660254037844386f; // sqrt(3) / 2

		public static float GetPointyHexApothem(float radius)
		{
			return HalfSqrt3 * radius;
		}

		/// <summary>
		/// Vertex on XZ plane. Index 0 = top, then clockwise.
		/// </summary>
		public static Vector3 GetPointyHexVertexXZ(int index, float radius)
		{
			index = Mod(index, 6);

			float angleDeg = 90f - index * 60f;
			float angleRad = angleDeg * Mathf.Deg2Rad;

			float x = radius * Mathf.Cos(angleRad);
			float z = radius * Mathf.Sin(angleRad);

			return new Vector3(x, 0f, z);
		}

		/// <summary>
		/// Edge midpoint on XZ plane.
		/// Edge i is between vertex i and vertex i+1.
		/// </summary>
		public static Vector3 GetPointyHexEdgeMidpointXZ(int edgeIndex, float radius)
		{
			edgeIndex = Mod(edgeIndex, 6);

			Vector3 v0 = GetPointyHexVertexXZ(edgeIndex, radius);
			Vector3 v1 = GetPointyHexVertexXZ(edgeIndex + 1, radius);

			Vector3 mid = (v0 + v1) * 0.5f;
			return mid;
		}

		/// <summary>
		/// Z rotation in degrees so an object lies along the edge.
		/// Assumes the object's local +X axis points along its length.
		/// </summary>
		public static float GetPointyHexEdgeRotationDeg(int index)
		{
			index = Mod(index, 6);

			return index switch
			{
				0 => 30,
				1 => 90,
				2 => 150,
				3 => 210,
				4 => 270,
				5 => 330,
				_ => 0f
			};
		}

		/// <summary>
		/// Direction along the edge on XZ plane.
		/// </summary>
		public static Vector3 GetPointyHexEdgeDirectionXZ(int edgeIndex, float radius)
		{
			edgeIndex = Mod(edgeIndex, 6);

			Vector3 v0 = GetPointyHexVertexXZ(edgeIndex, radius);
			Vector3 v1 = GetPointyHexVertexXZ(edgeIndex + 1, radius);

			return (v1 - v0).normalized;
		}

		private static int Mod(int value, int modulo)
		{
			int r = value % modulo;
			return r < 0 ? r + modulo : r;
		}
	}
}
