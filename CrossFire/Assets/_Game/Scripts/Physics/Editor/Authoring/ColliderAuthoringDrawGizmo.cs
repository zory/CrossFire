using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Core.Physics
{
	public static class ColliderAuthoringDrawGizmo
	{
		[DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
		private static void Draw(ColliderAuthoring authoring, GizmoType gizmoType)
		{
			DrawAuthoringGizmos(authoring);
		}

		private static void DrawAuthoringGizmos(ColliderAuthoring authoring)
		{
			if (authoring == null)
			{
				return;
			}

			Matrix4x4 oldMatrix = Handles.matrix;
			Color oldColor = Handles.color;

			Handles.matrix = authoring.transform.localToWorldMatrix;

			if (authoring.ColliderType == Collider2DType.Circle)
			{
				if (authoring.ColliderCircleRadius > 0f)
				{
					DrawWireCircle(
						Vector3.zero,
						authoring.ColliderCircleRadius,
						CollisionDebugSettings.CircleShapeColor);
				}

				// For circle mode, bound and circle are the same thing.
				// Do not draw a second duplicate circle.
			}
			else if (authoring.ColliderType == Collider2DType.ConcaveTriangles)
			{
				if (authoring.ColliderBoundRadius > 0f)
				{
					DrawWireCircle(
						Vector3.zero,
						authoring.ColliderBoundRadius,
						CollisionDebugSettings.BoundRadiusColor);
				}

				float2[] outline = authoring.OutlineVertices;
				if (outline == null || outline.Length < 2)
				{
					return;
				}

				DrawOutline(
					outline,
					CollisionDebugSettings.ConcaveShapeColor);

				if (outline.Length >= 3)
				{
					List<float2> triangles = PhysicsUtilities.Triangulate(outline);
					DrawTriangleSoup(
						triangles,
						CollisionDebugSettings.TrianglesPreviewColor);
				}
			}

			Handles.color = oldColor;
			Handles.matrix = oldMatrix;
		}

		private static void DrawWireCircle(Vector3 center, float radius, Color color)
		{
			if (radius <= 0f)
			{
				return;
			}

			Handles.color = color;

			GeometryUtilities.ForEachCircleSegment(
				new float2(center.x, center.y),
				radius,
				CollisionDebugSettings.CircleSegments,
				(a, b) => Handles.DrawLine(
					new Vector3(a.x, a.y, center.z),
					new Vector3(b.x, b.y, center.z)
				)
			);
		}

		private static void DrawOutline(float2[] outline, Color color)
		{
			if (outline == null || outline.Length < 2)
			{
				return;
			}

			Handles.color = color;

			for (int i = 0; i < outline.Length; i++)
			{
				float3 a = PhysicsUtilities.ToFloat3(outline[i], 0);
				float3 b = PhysicsUtilities.ToFloat3(outline[(i + 1) % outline.Length], 0);
				Handles.DrawLine(a, b);
			}
		}

		private static void DrawTriangleSoup(List<float2> triangles, Color color)
		{
			if (triangles == null || triangles.Count < 3)
			{
				return;
			}

			Handles.color = color;

			for (int i = 0; i + 2 < triangles.Count; i += 3)
			{
				Vector3 a = new Vector3(triangles[i + 0].x, triangles[i + 0].y, 0f);
				Vector3 b = new Vector3(triangles[i + 1].x, triangles[i + 1].y, 0f);
				Vector3 c = new Vector3(triangles[i + 2].x, triangles[i + 2].y, 0f);

				Handles.DrawLine(a, b);
				Handles.DrawLine(b, c);
				Handles.DrawLine(c, a);
			}
		}
	}
}