using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	public static class GeometryUtilities
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static void ForEachCircleSegment(
			float2 center, float radius, int segments,
			Action<float2, float2> drawLineCallback)
		{
			if (radius <= 0f || segments < 3 || drawLineCallback == null)
			{
				return;
			}

			float step = math.PI * 2f / segments;
			float2 previousPoint = center + new float2(math.cos(0f), math.sin(0f)) * radius;

			for (int index = 1; index <= segments; index++)
			{
				float angle = index * step;
				float2 nextPoint = center + new float2(math.cos(angle), math.sin(angle)) * radius;
				drawLineCallback?.Invoke(previousPoint, nextPoint);
				previousPoint = nextPoint;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static void ForEachTriangleSoupEdgeWorld(
			ref BlobArray<float2> trianglesLocal,
			float2 positionWorld, float rotationRadians,
			Action<float2, float2> drawLineCallback)
		{
			float rotationCosine = math.cos(rotationRadians);
			float rotationSine = math.sin(rotationRadians);

			for (int triangleStartIndex = 0;
				 triangleStartIndex + 2 < trianglesLocal.Length;
				 triangleStartIndex += 3)
			{
				float2 a = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 0], rotationCosine, rotationSine) + positionWorld;
				float2 b = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 1], rotationCosine, rotationSine) + positionWorld;
				float2 c = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 2], rotationCosine, rotationSine) + positionWorld;

				drawLineCallback?.Invoke(a, b);
				drawLineCallback?.Invoke(b, c);
				drawLineCallback?.Invoke(c, a);
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static void ForSingleTriangleEdgeWorld(
			ref BlobArray<float2> trianglesLocal,
			int startIndex,
			float2 positionWorld, float rotationRadians,
			Action<float2, float2> drawLineCallback)
		{
			if (startIndex < 0 || startIndex + 2 >= trianglesLocal.Length)
			{
				return;
			}

			float rotationCosine = math.cos(rotationRadians);
			float rotationSine = math.sin(rotationRadians);

			float2 a = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 0], rotationCosine, rotationSine) + positionWorld;
			float2 b = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 1], rotationCosine, rotationSine) + positionWorld;
			float2 c = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 2], rotationCosine, rotationSine) + positionWorld;

			drawLineCallback?.Invoke(a, b);
			drawLineCallback?.Invoke(b, c);
			drawLineCallback?.Invoke(c, a);
		}




		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(
			in Collider2D firstCollider, in WorldPose firstWorldPose, in ConcaveTrianglesRef firstTriangleReference,
			in Collider2D secondCollider, in WorldPose secondWorldPose, in ConcaveTrianglesRef secondTriangleReference)
		{
			if (firstCollider.Type == Collider2DType.Circle && secondCollider.Type == Collider2DType.Circle)
			{
				return CircleIntersectsCircle(
					firstWorldPose.Value.Position, math.max(0f, firstCollider.CircleRadius),
					secondWorldPose.Value.Position, math.max(0f, secondCollider.CircleRadius));
			}

			if (firstCollider.Type == Collider2DType.Circle && secondCollider.Type == Collider2DType.ConcaveTriangles)
			{
				if (!secondTriangleReference.Value.IsCreated)
				{
					return false;
				}

				return CircleIntersectsTriangleSoupWorld(
					firstWorldPose.Value.Position, math.max(0f, firstCollider.CircleRadius),
					secondWorldPose.Value.Position, secondWorldPose.Value.ThetaRad, ref secondTriangleReference.Value.Value.Vertices);
			}

			if (firstCollider.Type == Collider2DType.ConcaveTriangles && secondCollider.Type == Collider2DType.Circle)
			{
				if (!firstTriangleReference.Value.IsCreated)
				{
					return false;
				}

				return CircleIntersectsTriangleSoupWorld(
					secondWorldPose.Value.Position, math.max(0f, secondCollider.CircleRadius),
					firstWorldPose.Value.Position, firstWorldPose.Value.ThetaRad, ref firstTriangleReference.Value.Value.Vertices);
			}

			// TODO: Triangle-vs-triangle not implemented yet.
			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsTriangleSoupWorld(
			float2 circleCenterWorld, float circleRadius,
			float2 targetPositionWorld, float targetRotationRad, ref BlobArray<float2> trianglesLocal)
		{
			if (circleRadius < 0f)
			{
				circleRadius = 0f;
			}

			float circleRadiusSquared = circleRadius * circleRadius;
			float rotationCosine = math.cos(targetRotationRad);
			float rotationSine = math.sin(targetRotationRad);

			for (int triangleVertexStartIndex = 0; triangleVertexStartIndex < trianglesLocal.Length; triangleVertexStartIndex += 3)
			{
				float2 firstTriangleVertexLocal = trianglesLocal[triangleVertexStartIndex + 0];
				float2 secondTriangleVertexLocal = trianglesLocal[triangleVertexStartIndex + 1];
				float2 thirdTriangleVertexLocal = trianglesLocal[triangleVertexStartIndex + 2];

				float2 firstTriangleVertexWorld = PhysicsUtilities.Rotate(firstTriangleVertexLocal, rotationCosine, rotationSine) + targetPositionWorld;
				float2 secondTriangleVertexWorld = PhysicsUtilities.Rotate(secondTriangleVertexLocal, rotationCosine, rotationSine) + targetPositionWorld;
				float2 thirdTriangleVertexWorld = PhysicsUtilities.Rotate(thirdTriangleVertexLocal, rotationCosine, rotationSine) + targetPositionWorld;

				bool circleIntersectsTriangle = CircleIntersectsTriangleWorld(
					circleCenterWorld, circleRadiusSquared,
					firstTriangleVertexWorld, secondTriangleVertexWorld, thirdTriangleVertexWorld);

				if (circleIntersectsTriangle)
				{
					return true;
				}
			}

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsTriangleWorld(
				float2 circleCenterWorld, float circleRadiusSquared,
				float2 a, float2 b, float2 c)
		{
			if (PhysicsUtilities.PointInTriangle(circleCenterWorld, a, b, c))
			{
				return true;
			}

			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, a, b) <= circleRadiusSquared)
			{
				return true;
			}
			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, b, c) <= circleRadiusSquared)
			{
				return true;
			}
			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, c, a) <= circleRadiusSquared)
			{
				return true;
			}

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsCircle(
			float2 firstCircleCenterWorld, float firstCircleRadius,
			float2 secondCircleCenterWorld, float secondCircleRadius)
		{
			float2 deltaBetweenCenters = secondCircleCenterWorld - firstCircleCenterWorld;
			float combinedRadius = math.max(0f, firstCircleRadius) + math.max(0f, secondCircleRadius);
			float combinedRadiusSquared = combinedRadius * combinedRadius;
			float distanceSquaredBetweenCenters = math.dot(deltaBetweenCenters, deltaBetweenCenters);
			if (distanceSquaredBetweenCenters > combinedRadiusSquared)
			{
				return false;
			}
			return true;
		}
	}

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
			float rotatedX = point.x * cosine - point.y * sine;
			float rotatedY = point.x * sine + point.y * cosine;

			return new float2(rotatedX, rotatedY);
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
			float cross1 = Cross(b - a, point - a);
			float cross2 = Cross(c - b, point - b);
			float cross3 = Cross(a - c, point - c);

			bool hasNegativeCross = (cross1 < 0f) || (cross2 < 0f) || (cross3 < 0f);
			bool hasPositiveCross = (cross1 > 0f) || (cross2 > 0f) || (cross3 > 0f);

			return !(hasNegativeCross && hasPositiveCross);
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

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static List<float2> Triangulate(float2[] outline)
		{
			var result = new List<float2>();
			int n = outline.Length;
			if (n < 3)
			{
				return result;
			}

			List<int> indices = new List<int>(n);
			for (int i = 0; i < n; i++)
			{
				indices.Add(i);
			}

			if (SignedArea(outline) < 0f)
			{
				indices.Reverse();
			}

			int guard = 0;
			while (indices.Count > 3 && guard < 10000)
			{
				guard++;

				bool earFound = false;

				for (int i = 0; i < indices.Count; i++)
				{
					int prevIndex = indices[(i - 1 + indices.Count) % indices.Count];
					int currIndex = indices[i];
					int nextIndex = indices[(i + 1) % indices.Count];

					float2 a = outline[prevIndex];
					float2 b = outline[currIndex];
					float2 c = outline[nextIndex];

					if (!IsConvex(a, b, c))
					{
						continue;
					}

					bool containsOtherPoint = false;
					for (int j = 0; j < indices.Count; j++)
					{
						int testIndex = indices[j];
						if (testIndex == prevIndex || testIndex == currIndex || testIndex == nextIndex)
						{
							continue;
						}

						float2 p = outline[testIndex];
						if (PointInTriangle(p, a, b, c))
						{
							containsOtherPoint = true;
							break;
						}
					}

					if (containsOtherPoint)
					{
						continue;
					}

					result.Add(a);
					result.Add(b);
					result.Add(c);

					indices.RemoveAt(i);
					earFound = true;
					break;
				}

				if (!earFound)
				{
					result.Clear();
					return result;
				}
			}

			if (indices.Count == 3)
			{
				result.Add(outline[indices[0]]);
				result.Add(outline[indices[1]]);
				result.Add(outline[indices[2]]);
			}

			return result;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float SignedArea(float2[] verts)
		{
			float area = 0f;
			for (int i = 0; i < verts.Length; i++)
			{
				float2 a = verts[i];
				float2 b = verts[(i + 1) % verts.Length];
				area += a.x * b.y - b.x * a.y;
			}

			return area * 0.5f;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool IsConvex(float2 a, float2 b, float2 c)
		{
			return Cross(b - a, c - b) > 0f;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float SqrMagnitude(float2 v)
		{
			return v.x * v.x + v.y * v.y;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float Cross(float2 left, float2 right)
		{
			return left.x * right.y - left.y * right.x;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static float3 ToFloat3(float2 point, float z)
		{
			return new float3(point.x, point.y, z);
		}

		public static float2 Forward(float rotationRadians)
		{
			return new float2(-math.sin(rotationRadians), math.cos(rotationRadians));
		}
	}
}