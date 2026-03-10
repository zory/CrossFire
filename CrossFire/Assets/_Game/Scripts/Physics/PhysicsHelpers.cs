using Unity.Mathematics;

namespace CrossFire.Physics
{
	public class PhysicsUtilities
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate(float2 point, float radians)
		{
			float cos = math.cos(radians);
			float sin = math.sin(radians);
			return Rotate(point, cos, sin);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate(float2 point, float cosine, float sine)
		{
			return new float2(
				point.x * cosine - point.y * sine,
				point.x * sine + point.y * cosine);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float2 TransformPoint(float2 localPoint, float2 worldPosition, float radians)
		{
			return Rotate(localPoint, radians) + worldPosition;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquaredPointToSegment(float2 point, float2 segmentStart, float2 segmentEnd)
		{
			float2 segmentVector = segmentEnd - segmentStart;
			float segmentLengthSquared = math.max(1e-12f, math.dot(segmentVector, segmentVector));
			float projectionFactor = math.clamp(
				math.dot(point - segmentStart, segmentVector) / segmentLengthSquared, 
				0f, 
				1f);

			float2 closestPoint = segmentStart + segmentVector * projectionFactor;
			float2 delta = point - closestPoint;
			return math.dot(delta, delta);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool PointInTriangle(float2 point, float2 a, float2 b, float2 c)
		{
			float s1 = Cross(b - a, point - a);
			float s2 = Cross(c - b, point - b);
			float s3 = Cross(a - c, point - c);

			bool hasNegative = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
			bool hasPositive = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);

			return !(hasNegative && hasPositive);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float Cross(float2 left, float2 right)
		{
			return left.x * right.y - left.y * right.x;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CanCollide(
			in CollisionLayer firstCollisionLayer,
			in CollisionMask firstCollisionMask,
			in CollisionLayer secondCollisionLayer,
			in CollisionMask secondCollisionMask)
		{
			bool firstCanCollideWithSecond = (firstCollisionMask.Value & secondCollisionLayer.Value) != 0u;
			bool secondCanCollideWithFirst = (secondCollisionMask.Value & firstCollisionLayer.Value) != 0u;

			if (!firstCanCollideWithSecond)
			{
				return false;
			}

			if (!secondCanCollideWithFirst)
			{
				return false;
			}

			return true;
		}
	}
}