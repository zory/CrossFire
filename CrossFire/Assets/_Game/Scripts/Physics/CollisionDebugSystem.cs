using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
	//For now only draws hardcoded bullets vs targets
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial struct CollisionDebugSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
#if !UNITY_EDITOR
			return;
#else
			if (!Application.isPlaying)
			{
				return;
			}

			if (!CollisionDebugSettings.Enabled)
			{
				return;
			}

			EntityManager entityManager = state.EntityManager;

			using var gridQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CollisionGridSettings>());
			if (gridQuery.IsEmptyIgnoreFilter)
			{
				return;
			}

			DrawCollisionDebug(entityManager);
#endif
		}

		private static void DrawCollisionDebug(EntityManager entityManager)
		{
			float invCell = 1f;

			using (var gridQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CollisionGridSettings>()))
			{
				if (!gridQuery.IsEmptyIgnoreFilter)
				{
					CollisionGridSettings collisionGridSettings = gridQuery.GetSingleton<CollisionGridSettings>();
					float cellSize = math.max(0.0001f, collisionGridSettings.CellSize);
					invCell = 1f / cellSize;
				}
			}

			using var targetQuery = entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<CollisionLayer>(),
				ComponentType.ReadOnly<CollisionMask>(),
				ComponentType.ReadOnly<Collider2D>(),
				ComponentType.ReadOnly<ConcaveTrianglesRef>());

			using var bulletQuery = entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<CollisionLayer>(),
				ComponentType.ReadOnly<CollisionMask>(),
				ComponentType.ReadOnly<Collider2D>());

			using var tEntities = targetQuery.ToEntityArray(Allocator.Temp);
			using var tPoses = targetQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var tLayers = targetQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using var tMasks = targetQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using var tColliders = targetQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);
			using var tTriRefs = targetQuery.ToComponentDataArray<ConcaveTrianglesRef>(Allocator.Temp);

			using var bEntities = bulletQuery.ToEntityArray(Allocator.Temp);
			using var bPoses = bulletQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var bLayers = bulletQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using var bMasks = bulletQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using var bColliders = bulletQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);

			var grid = new NativeParallelMultiHashMap<int, int>(math.max(1, tEntities.Length * 2), Allocator.Temp);

			for (int i = 0; i < tEntities.Length; i++)
			{
				float2 p = tPoses[i].Value.Position;
				int2 cell = (int2)math.floor(p * invCell);
				grid.Add(Hash(cell), i);
			}

			for (int i = 0; i < tEntities.Length; i++)
			{
				float2 tp = tPoses[i].Value.Position;
				float tr = math.max(0f, tColliders[i].BoundRadius);

				DrawCircle(tp, tr, Color.yellow, CollisionDebugSettings.CircleSegments, CollisionDebugSettings.ZOffset);

				var triRef = tTriRefs[i].Value;
				if (triRef.IsCreated)
				{
					DrawTriangleSoupWorld(
						ref triRef.Value.Vertices,
						tp,
						tPoses[i].Value.Theta,
						Color.green,
						CollisionDebugSettings.ZOffset);
				}
			}

			for (int i = 0; i < bEntities.Length; i++)
			{
				float2 bp = bPoses[i].Value.Position;
				float br = math.max(0f, bColliders[i].CircleRadius);
				if (br <= 0f)
					br = math.max(0f, bColliders[i].BoundRadius);

				DrawCircle(bp, br, Color.cyan, CollisionDebugSettings.CircleSegments, CollisionDebugSettings.ZOffset);
			}

			for (int bi = 0; bi < bEntities.Length; bi++)
			{
				float2 bp = bPoses[bi].Value.Position;
				float br = math.max(0f, bColliders[bi].CircleRadius);
				if (br <= 0f)
					br = math.max(0f, bColliders[bi].BoundRadius);

				var bLayer = bLayers[bi];
				var bMask = bMasks[bi];

				int2 baseCell = (int2)math.floor(bp * invCell);

				for (int oy = -1; oy <= 1; oy++)
					for (int ox = -1; ox <= 1; ox++)
					{
						int2 c = baseCell + new int2(ox, oy);
						int key = Hash(c);

						var it = grid.GetValuesForKey(key);
						while (it.MoveNext())
						{
							int ti = it.Current;

							if (!CollisionFilterUtil.CanCollide(bLayer, bMask, tLayers[ti], tMasks[ti]))
								continue;

							float2 tp = tPoses[ti].Value.Position;
							float tr = math.max(0f, tColliders[ti].BoundRadius);

							float2 d = tp - bp;
							float rr = tr + br;
							bool broadphasePass = math.dot(d, d) <= rr * rr;

							if (CollisionDebugSettings.DrawBroadphase && broadphasePass)
							{
								Debug.DrawLine(
									ToV3(bp, CollisionDebugSettings.ZOffset),
									ToV3(tp, CollisionDebugSettings.ZOffset),
									new Color(1f, 0.5f, 0f, 1f));
							}

							if (!broadphasePass)
								continue;

							var triRef = tTriRefs[ti].Value;
							if (!triRef.IsCreated)
								continue;

							int hitTriIndex;
							bool hit = TryFindHitTriangle(
								bp,
								br,
								ref triRef.Value.Vertices,
								tp,
								tPoses[ti].Value.Theta,
								out hitTriIndex);

							if (hit && CollisionDebugSettings.DrawHitTriangles && hitTriIndex >= 0)
							{
								DrawSingleTriangleWorld(
									ref triRef.Value.Vertices,
									hitTriIndex,
									tp,
									tPoses[ti].Value.Theta,
									Color.red,
									CollisionDebugSettings.ZOffset);
							}
						}
					}
			}

			grid.Dispose();
		}

		private static bool TryFindHitTriangle(
			float2 circleCenterWorld,
			float circleRadius,
			ref BlobArray<float2> trisLocal,
			float2 targetPosWorld,
			float targetRotDeg,
			out int hitTriStartIndex)
		{
			hitTriStartIndex = -1;

			float rotRad = targetRotDeg * math.TORADIANS;
			float c = math.cos(rotRad);
			float s = math.sin(rotRad);
			float r2 = circleRadius * circleRadius;

			for (int i = 0; i < trisLocal.Length; i += 3)
			{
				float2 a = Rotate(trisLocal[i + 0], c, s) + targetPosWorld;
				float2 b = Rotate(trisLocal[i + 1], c, s) + targetPosWorld;
				float2 cc = Rotate(trisLocal[i + 2], c, s) + targetPosWorld;

				if (CircleIntersectsTriangleWorld(circleCenterWorld, r2, a, b, cc))
				{
					hitTriStartIndex = i;
					return true;
				}
			}

			return false;
		}

		private static void DrawTriangleSoupWorld(
			ref BlobArray<float2> trisLocal,
			float2 pos,
			float rotDeg,
			Color color,
			float z)
		{
			float rotRad = rotDeg * math.TORADIANS;
			float c = math.cos(rotRad);
			float s = math.sin(rotRad);

			for (int i = 0; i + 2 < trisLocal.Length; i += 3)
			{
				float2 a = Rotate(trisLocal[i + 0], c, s) + pos;
				float2 b = Rotate(trisLocal[i + 1], c, s) + pos;
				float2 cc = Rotate(trisLocal[i + 2], c, s) + pos;

				Debug.DrawLine(ToV3(a, z), ToV3(b, z), color);
				Debug.DrawLine(ToV3(b, z), ToV3(cc, z), color);
				Debug.DrawLine(ToV3(cc, z), ToV3(a, z), color);
			}
		}

		private static void DrawSingleTriangleWorld(
			ref BlobArray<float2> trisLocal,
			int startIndex,
			float2 pos,
			float rotDeg,
			Color color,
			float z)
		{
			if (startIndex < 0 || startIndex + 2 >= trisLocal.Length)
				return;

			float rotRad = rotDeg * math.TORADIANS;
			float c = math.cos(rotRad);
			float s = math.sin(rotRad);

			float2 a = Rotate(trisLocal[startIndex + 0], c, s) + pos;
			float2 b = Rotate(trisLocal[startIndex + 1], c, s) + pos;
			float2 cc = Rotate(trisLocal[startIndex + 2], c, s) + pos;

			Debug.DrawLine(ToV3(a, z), ToV3(b, z), color);
			Debug.DrawLine(ToV3(b, z), ToV3(cc, z), color);
			Debug.DrawLine(ToV3(cc, z), ToV3(a, z), color);
		}

		private static void DrawCircle(float2 center, float radius, Color color, int segments, float z)
		{
			if (radius <= 0f || segments < 3)
				return;

			float step = math.PI * 2f / segments;
			float2 prev = center + new float2(math.cos(0f), math.sin(0f)) * radius;

			for (int i = 1; i <= segments; i++)
			{
				float a = i * step;
				float2 next = center + new float2(math.cos(a), math.sin(a)) * radius;
				Debug.DrawLine(ToV3(prev, z), ToV3(next, z), color);
				prev = next;
			}
		}

		private static float2 Rotate(float2 p, float c, float s)
		{
			return new float2(
				p.x * c - p.y * s,
				p.x * s + p.y * c
			);
		}

		private static bool CircleIntersectsTriangleWorld(float2 p, float r2, float2 a, float2 b, float2 c)
		{
			if (PointInTriangle(p, a, b, c))
				return true;

			if (DistSqPointSegment(p, a, b) <= r2) return true;
			if (DistSqPointSegment(p, b, c) <= r2) return true;
			if (DistSqPointSegment(p, c, a) <= r2) return true;

			return false;
		}

		private static float DistSqPointSegment(float2 p, float2 a, float2 b)
		{
			float2 ab = b - a;
			float abLenSq = math.max(1e-12f, math.dot(ab, ab));
			float t = math.clamp(math.dot(p - a, ab) / abLenSq, 0f, 1f);
			float2 q = a + ab * t;
			float2 d = p - q;
			return math.dot(d, d);
		}

		private static bool PointInTriangle(float2 p, float2 a, float2 b, float2 c)
		{
			float s1 = Cross(b - a, p - a);
			float s2 = Cross(c - b, p - b);
			float s3 = Cross(a - c, p - c);

			bool hasNeg = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
			bool hasPos = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);

			return !(hasNeg && hasPos);
		}

		private static float Cross(float2 u, float2 v)
		{
			return u.x * v.y - u.y * v.x;
		}

		private static Vector3 ToV3(float2 p, float z)
		{
			return new Vector3(p.x, p.y, z);
		}

		private static int Hash(int2 cell)
		{
			return (cell.x * 73856093) ^ (cell.y * 19349663);
		}
	}
}