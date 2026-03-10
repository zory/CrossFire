using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
	public static class Narrowphase2D
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(
			in Collider2D firstCollider,
			in WorldPose firstWorldPose,
			in ConcaveTrianglesRef firstTriangleReference,
			in Collider2D secondCollider,
			in WorldPose secondWorldPose,
			in ConcaveTrianglesRef secondTriangleReference)
		{
			if (firstCollider.Type == Collider2DType.Circle &&
				secondCollider.Type == Collider2DType.Circle)
			{
				return CircleIntersectsCircle(
					firstWorldPose.Value.Position,
					math.max(0f, firstCollider.CircleRadius),
					secondWorldPose.Value.Position,
					math.max(0f, secondCollider.CircleRadius));
			}

			if (firstCollider.Type == Collider2DType.Circle &&
				secondCollider.Type == Collider2DType.ConcaveTriangles)
			{
				if (!secondTriangleReference.Value.IsCreated)
				{
					return false;
				}

				return CircleIntersectsTriangleSoupWorld(
					firstWorldPose.Value.Position,
					math.max(0f, firstCollider.CircleRadius),
					ref secondTriangleReference.Value.Value.Vertices,
					secondWorldPose.Value.Position,
					secondWorldPose.Value.Theta);
			}

			if (firstCollider.Type == Collider2DType.ConcaveTriangles &&
				secondCollider.Type == Collider2DType.Circle)
			{
				if (!firstTriangleReference.Value.IsCreated)
				{
					return false;
				}

				return CircleIntersectsTriangleSoupWorld(
					secondWorldPose.Value.Position,
					math.max(0f, secondCollider.CircleRadius),
					ref firstTriangleReference.Value.Value.Vertices,
					firstWorldPose.Value.Position,
					firstWorldPose.Value.Theta);
			}

			// Triangle-vs-triangle not implemented yet.
			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsCircle(
			float2 firstCircleCenterWorld,
			float firstCircleRadius,
			float2 secondCircleCenterWorld,
			float secondCircleRadius)
		{
			float2 deltaBetweenCenters =
				secondCircleCenterWorld - firstCircleCenterWorld;

			float combinedRadius =
				math.max(0f, firstCircleRadius) + math.max(0f, secondCircleRadius);

			float combinedRadiusSquared =
				combinedRadius * combinedRadius;

			float distanceSquaredBetweenCenters =
				math.dot(deltaBetweenCenters, deltaBetweenCenters);

			if (distanceSquaredBetweenCenters > combinedRadiusSquared)
			{
				return false;
			}

			return true;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsTriangleSoupWorld(
			float2 circleCenterWorld,
			float circleRadius,
			ref BlobArray<float2> triangleVerticesLocalSpace,
			float2 targetPositionWorld,
			float targetRotationDegrees)
		{
			if (circleRadius < 0f)
			{
				circleRadius = 0f;
			}

			float circleRadiusSquared = circleRadius * circleRadius;

			float targetRotationRadians =
				targetRotationDegrees * math.TORADIANS;

			float rotationCosine =
				math.cos(targetRotationRadians);

			float rotationSine =
				math.sin(targetRotationRadians);

			for (int triangleVertexStartIndex = 0;
				 triangleVertexStartIndex < triangleVerticesLocalSpace.Length;
				 triangleVertexStartIndex += 3)
			{
				float2 firstTriangleVertexLocal =
					triangleVerticesLocalSpace[triangleVertexStartIndex + 0];

				float2 secondTriangleVertexLocal =
					triangleVerticesLocalSpace[triangleVertexStartIndex + 1];

				float2 thirdTriangleVertexLocal =
					triangleVerticesLocalSpace[triangleVertexStartIndex + 2];

				float2 firstTriangleVertexWorld =
					Rotate(firstTriangleVertexLocal, rotationCosine, rotationSine) +
					targetPositionWorld;

				float2 secondTriangleVertexWorld =
					Rotate(secondTriangleVertexLocal, rotationCosine, rotationSine) +
					targetPositionWorld;

				float2 thirdTriangleVertexWorld =
					Rotate(thirdTriangleVertexLocal, rotationCosine, rotationSine) +
					targetPositionWorld;

				bool circleIntersectsTriangle = CircleIntersectsTriangleWorld(
					circleCenterWorld,
					circleRadiusSquared,
					firstTriangleVertexWorld,
					secondTriangleVertexWorld,
					thirdTriangleVertexWorld);

				if (circleIntersectsTriangle)
				{
					return true;
				}
			}

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static float2 Rotate(
			float2 point,
			float rotationCosine,
			float rotationSine)
		{
			float rotatedX =
				point.x * rotationCosine - point.y * rotationSine;

			float rotatedY =
				point.x * rotationSine + point.y * rotationCosine;

			return new float2(rotatedX, rotatedY);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool CircleIntersectsTriangleWorld(
			float2 circleCenterWorld,
			float circleRadiusSquared,
			float2 firstTriangleVertexWorld,
			float2 secondTriangleVertexWorld,
			float2 thirdTriangleVertexWorld)
		{
			bool circleCenterIsInsideTriangle = PointInTriangle(
				circleCenterWorld,
				firstTriangleVertexWorld,
				secondTriangleVertexWorld,
				thirdTriangleVertexWorld);

			if (circleCenterIsInsideTriangle)
			{
				return true;
			}

			float distanceSquaredToFirstEdge = DistanceSquaredPointToSegment(
				circleCenterWorld,
				firstTriangleVertexWorld,
				secondTriangleVertexWorld);

			if (distanceSquaredToFirstEdge <= circleRadiusSquared)
			{
				return true;
			}

			float distanceSquaredToSecondEdge = DistanceSquaredPointToSegment(
				circleCenterWorld,
				secondTriangleVertexWorld,
				thirdTriangleVertexWorld);

			if (distanceSquaredToSecondEdge <= circleRadiusSquared)
			{
				return true;
			}

			float distanceSquaredToThirdEdge = DistanceSquaredPointToSegment(
				circleCenterWorld,
				thirdTriangleVertexWorld,
				firstTriangleVertexWorld);

			if (distanceSquaredToThirdEdge <= circleRadiusSquared)
			{
				return true;
			}

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static float DistanceSquaredPointToSegment(
			float2 point,
			float2 segmentStart,
			float2 segmentEnd)
		{
			float2 segmentVector =
				segmentEnd - segmentStart;

			float segmentLengthSquared =
				math.max(1e-12f, math.dot(segmentVector, segmentVector));

			float projectionFactor =
				math.dot(point - segmentStart, segmentVector) / segmentLengthSquared;

			float clampedProjectionFactor =
				math.clamp(projectionFactor, 0f, 1f);

			float2 closestPointOnSegment =
				segmentStart + segmentVector * clampedProjectionFactor;

			float2 deltaToClosestPoint =
				point - closestPointOnSegment;

			return math.dot(deltaToClosestPoint, deltaToClosestPoint);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool PointInTriangle(
			float2 point,
			float2 firstTriangleVertex,
			float2 secondTriangleVertex,
			float2 thirdTriangleVertex)
		{
			float firstCross = Cross(
				secondTriangleVertex - firstTriangleVertex,
				point - firstTriangleVertex);

			float secondCross = Cross(
				thirdTriangleVertex - secondTriangleVertex,
				point - secondTriangleVertex);

			float thirdCross = Cross(
				firstTriangleVertex - thirdTriangleVertex,
				point - thirdTriangleVertex);

			bool hasNegativeCross =
				(firstCross < 0f) ||
				(secondCross < 0f) ||
				(thirdCross < 0f);

			bool hasPositiveCross =
				(firstCross > 0f) ||
				(secondCross > 0f) ||
				(thirdCross > 0f);

			if (hasNegativeCross && hasPositiveCross)
			{
				return false;
			}

			return true;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static float Cross(
			float2 firstVector,
			float2 secondVector)
		{
			return firstVector.x * secondVector.y - firstVector.y * secondVector.x;
		}
	}
}