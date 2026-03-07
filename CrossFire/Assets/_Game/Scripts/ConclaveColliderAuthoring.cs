using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire
{
	public enum Collider2DType : byte
	{
		Circle = 0,
		ConcaveTriangles = 1
	}

	public class ConcaveColliderAuthoring : MonoBehaviour
	{
		public Collider2DType ColliderType = Collider2DType.ConcaveTriangles;

		public bool AutoCalculateBoundRadius = true;

		[Min(0f)]
		public float ColliderBoundRadius = 1f;

		[Min(0f)]
		public float ColliderCircleRadius = 0.5f;

		// Polygon OUTLINE in local space, in winding order.
		// No holes. For concave shapes, triangulation happens automatically in baker.
		public Vector2[] OutlineVertices;

		public bool DrawBoundRadius = true;
		public bool DrawCircleRadius = true;
		public bool DrawOutline = true;
		public bool DrawTrianglesPreview = true;

		private void OnValidate()
		{
			ColliderCircleRadius = Mathf.Max(0f, ColliderCircleRadius);

			if (AutoCalculateBoundRadius)
				ColliderBoundRadius = CalculateBoundRadius();
			else
				ColliderBoundRadius = Mathf.Max(0f, ColliderBoundRadius);
		}

		public float CalculateBoundRadius()
		{
			if (ColliderType == Collider2DType.Circle)
				return Mathf.Max(0f, ColliderCircleRadius);

			float maxSq = 0f;

			if (OutlineVertices != null)
			{
				for (int i = 0; i < OutlineVertices.Length; i++)
				{
					float sq = OutlineVertices[i].sqrMagnitude;
					if (sq > maxSq)
						maxSq = sq;
				}
			}

			return Mathf.Sqrt(maxSq);
		}

		class ConcaveColliderBaker : Baker<ConcaveColliderAuthoring>
		{
			public override void Bake(ConcaveColliderAuthoring authoring)
			{
				var entity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent(entity, new Collider2D
				{
					Type = (Collider2DType)authoring.ColliderType,
					BoundRadius = authoring.ColliderBoundRadius,
					CircleRadius = authoring.ColliderCircleRadius
				});

				if (authoring.ColliderType != Collider2DType.ConcaveTriangles)
					return;

				if (authoring.OutlineVertices == null || authoring.OutlineVertices.Length < 3)
					return;

				List<float2> triangleSoup = Triangulate(authoring.OutlineVertices);
				if (triangleSoup.Count == 0)
					return;

				using var builder = new BlobBuilder(Allocator.Temp);
				ref var root = ref builder.ConstructRoot<TriangleSoupBlob>();

				var verts = builder.Allocate(ref root.Vertices, triangleSoup.Count);
				for (int i = 0; i < triangleSoup.Count; i++)
				{
					verts[i] = triangleSoup[i];
				}

				var blob = builder.CreateBlobAssetReference<TriangleSoupBlob>(Allocator.Persistent);

				AddComponent(entity, new ConcaveTrianglesRef
				{
					Value = blob
				});

				AddBlobAsset(ref blob, out _);
			}

			private static List<float2> Triangulate(Vector2[] outline)
			{
				var result = new List<float2>();
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

						float2 a = outline[prevIndex];
						float2 b = outline[currIndex];
						float2 c = outline[nextIndex];

						if (!IsConvex(a, b, c))
							continue;

						bool containsOtherPoint = false;
						for (int j = 0; j < indices.Count; j++)
						{
							int testIndex = indices[j];
							if (testIndex == prevIndex || testIndex == currIndex || testIndex == nextIndex)
								continue;

							float2 p = outline[testIndex];
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

			private static bool IsConvex(float2 a, float2 b, float2 c)
			{
				return Cross(b - a, c - b) > 0f;
			}

			private static float Cross(float2 u, float2 v)
			{
				return u.x * v.y - u.y * v.x;
			}

			private static bool PointInTriangle(float2 p, float2 a, float2 b, float2 c)
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
}