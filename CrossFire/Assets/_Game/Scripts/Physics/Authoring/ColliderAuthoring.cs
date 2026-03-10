using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
	public class ColliderAuthoring : MonoBehaviour
	{
		public Collider2DType ColliderType = Collider2DType.ConcaveTriangles;

		// Polygon OUTLINE in local space, in winding order.
		// No holes. For concave shapes, triangulation happens automatically in baker.
		// Only with concave triangles mode
		public float2[] OutlineVertices;

		// Used for concave triangles and is autocalculated
		[Min(0f)]
		public float ColliderBoundRadius = 1f;

		// Used only for circle collider shape.
		[Min(0f)]
		public float ColliderCircleRadius = 0.5f;

		private void OnValidate()
		{
			if (ColliderType == Collider2DType.Circle)
			{
				ColliderCircleRadius = Mathf.Max(0f, ColliderCircleRadius);
				ColliderBoundRadius = ColliderCircleRadius;
			}
			else
			{
				ColliderBoundRadius = CalculateBoundRadius();
			}
		}

		public float CalculateBoundRadius()
		{
			if (ColliderType == Collider2DType.Circle)
			{
				return Mathf.Max(0f, ColliderCircleRadius);
			}

			float maxSq = 0f;

			if (OutlineVertices != null)
			{
				for (int i = 0; i < OutlineVertices.Length; i++)
				{
					float sq = PhysicsUtilities.SqrMagnitude(OutlineVertices[i]);
					if (sq > maxSq)
					{
						maxSq = sq;
					}
				}
			}

			return Mathf.Sqrt(maxSq);
		}

		class ConcaveColliderBaker : Baker<ColliderAuthoring>
		{
			public override void Bake(ColliderAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);

				bool isCircleCollider = authoring.ColliderType == Collider2DType.Circle;

				float boundRadius = isCircleCollider ? authoring.ColliderCircleRadius : authoring.ColliderBoundRadius;
				boundRadius = math.max(0f, boundRadius);

				float circleRadius = isCircleCollider ? authoring.ColliderCircleRadius : 0f;
				circleRadius = math.max(0f, circleRadius);

				AddComponent(entity, new Collider2D
				{
					Type = authoring.ColliderType,
					BoundRadius = boundRadius,
					CircleRadius = circleRadius
				});

				if (authoring.ColliderType != Collider2DType.ConcaveTriangles)
				{
					return;
				}

				if (authoring.OutlineVertices == null || authoring.OutlineVertices.Length < 3)
				{
					return;
				}

				List<float2> triangleSoup = PhysicsUtilities.Triangulate(authoring.OutlineVertices);
				if (triangleSoup.Count == 0)
				{
					return;
				}

				using BlobBuilder builder = new BlobBuilder(Allocator.Temp);
				ref TriangleSoupBlob root = ref builder.ConstructRoot<TriangleSoupBlob>();

				BlobBuilderArray<float2> verts = builder.Allocate(ref root.Vertices, triangleSoup.Count);
				for (int i = 0; i < triangleSoup.Count; i++)
				{
					verts[i] = triangleSoup[i];
				}

				BlobAssetReference<TriangleSoupBlob> blob = builder.CreateBlobAssetReference<TriangleSoupBlob>(Allocator.Persistent);

				AddComponent(entity, new ConcaveTrianglesRef
				{
					Value = blob
				});

				AddBlobAsset(ref blob, out _);
			}
		}
	}
}