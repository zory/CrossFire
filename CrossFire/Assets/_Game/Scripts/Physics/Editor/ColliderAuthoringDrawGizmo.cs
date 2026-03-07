using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrossFire.Physics
{
	public static class ColliderAuthoringDrawGizmo
	{
		[DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
		private static void Draw(ColliderAuthoring authoring, GizmoType gizmoType)
		{
			Transform t = authoring.transform;

			Matrix4x4 oldMatrix = Handles.matrix;
			Handles.matrix = t.localToWorldMatrix;

			if (authoring.DrawBoundRadius && authoring.ColliderBoundRadius > 0f)
			{
				Handles.color = new Color(1f, 1f, 0f, 0.95f);
				Handles.DrawWireDisc(Vector3.zero, Vector3.forward, authoring.ColliderBoundRadius);
			}

			if (authoring.ColliderType == Collider2DType.Circle)
			{
				if (authoring.DrawCircleRadius && authoring.ColliderCircleRadius > 0f)
				{
					Handles.color = new Color(0f, 1f, 1f, 0.95f);
					Handles.DrawWireDisc(Vector3.zero, Vector3.forward, authoring.ColliderCircleRadius);
				}
			}
			else if (authoring.ColliderType == Collider2DType.ConcaveTriangles)
			{
				if (authoring.OutlineVertices != null && authoring.OutlineVertices.Length >= 2)
				{
					if (authoring.DrawOutline)
					{
						Handles.color = new Color(0f, 1f, 0f, 0.95f);

						for (int i = 0; i < authoring.OutlineVertices.Length; i++)
						{
							Vector3 a = authoring.OutlineVertices[i];
							Vector3 b = authoring.OutlineVertices[(i + 1) % authoring.OutlineVertices.Length];
							Handles.DrawLine(a, b);
						}
					}

					if (authoring.DrawTrianglesPreview && authoring.OutlineVertices.Length >= 3)
					{
						List<Vector2> tris = Triangulate(authoring.OutlineVertices);

						Handles.color = new Color(0f, 0.7f, 1f, 0.8f);
						for (int i = 0; i + 2 < tris.Count; i += 3)
						{
							Vector3 a = tris[i + 0];
							Vector3 b = tris[i + 1];
							Vector3 c = tris[i + 2];

							Handles.DrawLine(a, b);
							Handles.DrawLine(b, c);
							Handles.DrawLine(c, a);
						}
					}
				}
			}

			Handles.matrix = oldMatrix;
		}

		private static List<Vector2> Triangulate(Vector2[] outline)
		{
			var result = new List<Vector2>();
			int n = outline.Length;
			if (n < 3)
				return result;

			List<int> indices = new List<int>(n);
			for (int i = 0; i < n; i++)
				indices.Add(i);

			if (SignedArea(outline) < 0f)
				indices.Reverse();

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

					Vector2 a = outline[prevIndex];
					Vector2 b = outline[currIndex];
					Vector2 c = outline[nextIndex];

					if (!IsConvex(a, b, c))
						continue;

					bool containsOtherPoint = false;
					for (int j = 0; j < indices.Count; j++)
					{
						int testIndex = indices[j];
						if (testIndex == prevIndex || testIndex == currIndex || testIndex == nextIndex)
							continue;

						Vector2 p = outline[testIndex];
						if (PointInTriangle(p, a, b, c))
						{
							containsOtherPoint = true;
							break;
						}
					}

					if (containsOtherPoint)
						continue;

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

		private static float SignedArea(Vector2[] verts)
		{
			float area = 0f;
			for (int i = 0; i < verts.Length; i++)
			{
				Vector2 a = verts[i];
				Vector2 b = verts[(i + 1) % verts.Length];
				area += a.x * b.y - b.x * a.y;
			}
			return area * 0.5f;
		}

		private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
		{
			return Cross(b - a, c - b) > 0f;
		}

		private static float Cross(Vector2 u, Vector2 v)
		{
			return u.x * v.y - u.y * v.x;
		}

		private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			float c1 = Cross(b - a, p - a);
			float c2 = Cross(c - b, p - b);
			float c3 = Cross(a - c, p - c);

			bool hasNeg = (c1 < 0f) || (c2 < 0f) || (c3 < 0f);
			bool hasPos = (c1 > 0f) || (c2 > 0f) || (c3 > 0f);

			return !(hasNeg && hasPos);
		}
	}
}