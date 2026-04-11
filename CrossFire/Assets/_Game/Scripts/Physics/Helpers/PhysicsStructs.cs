using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	[Serializable]
	public struct Pose2D
	{
		public float2 Position;
		public float ThetaRad;	//Radians

		public override string ToString()
		{
			return string.Format("[{0}|{1}]", Position, ThetaRad);
		}
	}

	public enum Collider2DType : byte
	{
		Circle = 0,
		ConcaveTriangles = 1
	}

	/// <summary>
	/// Local-space triangle soup. Every 3 vertices form 1 triangle, CCW or CW winding doesn't matter for distance tests.
	/// </summary>
	public struct TriangleSoupBlob
	{
		public BlobArray<float2> Vertices; // length % 3 == 0
	}
}
