using System;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Physics
{
	[Serializable]
	public struct Pose2D
	{
		public float2 Position;
		public float Theta;

		public override string ToString()
		{
			return string.Format("[{0}|{1}]", Position, Theta);
		}
	}

	public enum Collider2DType : byte
	{
		Circle = 0,
		ConcaveTriangles = 1
	}

	/// <summary>
	/// Local-space triangle soup. Every 3 vertices form 1 triangle, CCW or CW doesn’t matter for distance tests.
	/// </summary>
	public struct TriangleSoupBlob
	{
		public BlobArray<float2> Vertices; // length % 3 == 0
	}

	/// <summary>
	/// Hash map cell -> list of indices into a separate array is fast, but simplest is cell -> TargetEntry directly.
	/// </summary>
	public struct CellKey : System.IEquatable<CellKey>
	{
		public int2 Cell;
		public bool Equals(CellKey other)
		{
			return Cell.x == other.Cell.x && Cell.y == other.Cell.y;
		}

		public override int GetHashCode()
		{
			return (Cell.x * 73856093) ^ (Cell.y * 19349663);
		}
	}

	public struct BroadphasePair
	{
		public Entity FirstEntity;
		public Entity SecondEntity;
	}
}