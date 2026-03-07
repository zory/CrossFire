// Bullet-vs-ConcaveTargets collision for DOTS/ECS (2D).
// - Concave ships/walls are represented as a TRIANGLE LIST (no holes assumed).
// - Broadphase: uniform grid (spatial hash) of targets.
// - Narrowphase: circle (bullet) vs triangles (target).
// - Filtering: Layer/Mask bits (reusable for future collisions).
//
// You can reuse the same grid + filter + narrowphase dispatch for other collision pairs later.

namespace CrossFire.Physics
{
	using Unity.Burst;
	using Unity.Collections;
	using Unity.Entities;
	using Unity.Mathematics;

	#region Collision filtering (reusable)

	public struct CollisionLayer : IComponentData { public uint Value; } // one-hot
	public struct CollisionMask : IComponentData { public uint Value; } // bitset

	public static class CollisionFilterUtil
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CanCollide(in CollisionLayer aL, in CollisionMask aM, in CollisionLayer bL, in CollisionMask bM)
			=> ((aM.Value & bL.Value) != 0u) && ((bM.Value & aL.Value) != 0u);
	}

	#endregion

	#region Colliders (2D)

	public struct Collider2D : IComponentData
	{
		public Collider2DType Type;
		public float BoundRadius; // broadphase bound radius in WORLD units
		public float CircleRadius; // only used when Type == Circle
	}

	public struct ConcaveTrianglesRef : IComponentData
	{
		public BlobAssetReference<TriangleSoupBlob> Value;
	}

	// Local-space triangle soup. Every 3 vertices form 1 triangle, CCW or CW doesn’t matter for distance tests.
	public struct TriangleSoupBlob
	{
		public BlobArray<float2> Vertices; // length % 3 == 0
	}

	#endregion

	#region Pose (adapt to your WorldPose)


	// Minimal 2D pose used by the collision code.
	public struct Pose2
	{
		public float2 Position;
		public float RotationRadians;

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public float2x2 RotMtx()
		{
			float c = math.cos(RotationRadians);
			float s = math.sin(RotationRadians);

			// Standard 2D rotation matrix:
			// [ c -s ]
			// [ s  c ]
			return new float2x2(
				c, -s,
				s, c
			);
		}
	}

	#endregion


	#region Spatial hash grid (targets)

	public struct CollisionGridSettings : IComponentData
	{
		public float CellSize; // e.g. 2..8 world units depending on density/ship size
	}

	// Packed info stored per target in a cell
	public struct TargetEntry
	{
		public Entity Entity;
		public float2 Position;
		public float RotationRadians;
		public CollisionLayer Layer;
		public CollisionMask Mask;
		public Collider2D Collider;
		public ConcaveTrianglesRef Triangles;
	}

	// Hash map cell -> list of indices into a separate array is fast, but simplest is cell -> TargetEntry directly.
	public struct CellKey : System.IEquatable<CellKey>
	{
		public int2 C;
		public bool Equals(CellKey other) => C.x == other.C.x && C.y == other.C.y;
		public override int GetHashCode() => (C.x * 73856093) ^ (C.y * 19349663);
	}

	#endregion

	#region Narrowphase: circle vs triangles

	public static class Narrowphase2D
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CircleIntersectsTriangleSoupWorld(
			float2 circleCenterWorld,
			float circleRadius,
			ref BlobArray<float2> trisLocal,
			float2 targetPosWorld,
			float targetRotDeg)
		{
			if (circleRadius < 0f)
				circleRadius = 0f;

			float r2 = circleRadius * circleRadius;

			float rotRad = targetRotDeg * math.TORADIANS;
			float c = math.cos(rotRad);
			float s = math.sin(rotRad);

			for (int i = 0; i < trisLocal.Length; i += 3)
			{
				float2 aLocal = trisLocal[i + 0];
				float2 bLocal = trisLocal[i + 1];
				float2 cLocal = trisLocal[i + 2];

				float2 a = Rotate(aLocal, c, s) + targetPosWorld;
				float2 b = Rotate(bLocal, c, s) + targetPosWorld;
				float2 cTri = Rotate(cLocal, c, s) + targetPosWorld;

				if (CircleIntersectsTriangleWorld(circleCenterWorld, r2, a, b, cTri))
					return true;
			}

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static float2 Rotate(float2 p, float c, float s)
		{
			return new float2(
				p.x * c - p.y * s,
				p.x * s + p.y * c
			);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static bool CircleIntersectsTriangleWorld(float2 p, float r2, float2 a, float2 b, float2 c)
		{
			if (PointInTriangle(p, a, b, c))
				return true;

			if (DistSqPointSegment(p, a, b) <= r2) return true;
			if (DistSqPointSegment(p, b, c) <= r2) return true;
			if (DistSqPointSegment(p, c, a) <= r2) return true;

			return false;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static float DistSqPointSegment(float2 p, float2 a, float2 b)
		{
			float2 ab = b - a;
			float abLenSq = math.max(1e-12f, math.dot(ab, ab));
			float t = math.clamp(math.dot(p - a, ab) / abLenSq, 0f, 1f);
			float2 q = a + ab * t;
			float2 d = p - q;
			return math.dot(d, d);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static bool PointInTriangle(float2 p, float2 a, float2 b, float2 c)
		{
			float s1 = Cross(b - a, p - a);
			float s2 = Cross(c - b, p - b);
			float s3 = Cross(a - c, p - c);

			bool hasNeg = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
			bool hasPos = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);

			return !(hasNeg && hasPos);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static float Cross(float2 u, float2 v)
		{
			return u.x * v.y - u.y * v.x;
		}
	}

	#endregion

	#region System: bullets vs concave targets (ships + walls)

	/// Tag anything that bullets should test against (ships, walls, etc.)
	public struct BulletTargetTag : IComponentData { }

	[BurstCompile]
	public partial struct BulletVsConcaveTargetsSystem : ISystem
	{
		private EntityQuery _targetsQuery;

		public void OnCreate(ref SystemState state)
		{
			_targetsQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<BulletTargetTag>(),
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<CollisionLayer>(),
				ComponentType.ReadOnly<CollisionMask>(),
				ComponentType.ReadOnly<Collider2D>(),
				ComponentType.ReadOnly<ConcaveTrianglesRef>());

			state.RequireForUpdate<CollisionGridSettings>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;
			var gridSettings = SystemAPI.GetSingleton<CollisionGridSettings>();
			float cellSize = math.max(0.0001f, gridSettings.CellSize);
			float invCell = 1f / cellSize;

			// Snapshot targets once (ships + walls)
			using var tEntities = _targetsQuery.ToEntityArray(Allocator.Temp);
			using var tPoses = _targetsQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var tLayers = _targetsQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using var tMasks = _targetsQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using var tColliders = _targetsQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);
			using var tTriRefs = _targetsQuery.ToComponentDataArray<ConcaveTrianglesRef>(Allocator.Temp);

			// Build grid: cell -> list of target indices
			var grid = new NativeParallelMultiHashMap<int, int>(tEntities.Length * 2, Allocator.Temp);

			for (int i = 0; i < tEntities.Length; i++)
			{
				float2 p = tPoses[i].Value.Position;
				int2 cell = (int2)math.floor(p * invCell);
				int key = Hash(cell);
				grid.Add(key, i);
			}

			var ecb = new EntityCommandBuffer(Allocator.Temp);

			// Iterate bullets (circle bullets)
			foreach (var (bPoseRO, bLayerRO, bMaskRO, bColRO, bDmgRO, bEntity) in
					 SystemAPI.Query<
						 RefRO<WorldPose>,
						 RefRO<CollisionLayer>,
						 RefRO<CollisionMask>,
						 RefRO<Collider2D>,
						 RefRO<BulletDamage>>()
					 .WithAll<BulletTag>()
					 .WithEntityAccess())
			{
				float2 bp = bPoseRO.ValueRO.Value.Position;

				// Bullet collider assumed circle
				float br = math.max(0f, bColRO.ValueRO.CircleRadius);
				if (br <= 0f) br = math.max(0f, bColRO.ValueRO.BoundRadius);

				var bLayer = bLayerRO.ValueRO;
				var bMask = bMaskRO.ValueRO;

				// Scan 3x3 neighboring cells
				int2 baseCell = (int2)math.floor(bp * invCell);
				Entity hitTarget = Entity.Null;

				for (int oy = -1; oy <= 1 && hitTarget == Entity.Null; oy++)
					for (int ox = -1; ox <= 1 && hitTarget == Entity.Null; ox++)
					{
						int2 c = baseCell + new int2(ox, oy);
						int key = Hash(c);

						var it = grid.GetValuesForKey(key);
						while (it.MoveNext())
						{
							int ti = it.Current;

							// Filter (layer/mask)
							if (!CollisionFilterUtil.CanCollide(bLayer, bMask, tLayers[ti], tMasks[ti]))
								continue;

							// Broadphase bound circle
							float2 tp = tPoses[ti].Value.Position;
							float tr = math.max(0f, tColliders[ti].BoundRadius);
							float2 d = tp - bp;
							float rr = tr + br;
							if (math.dot(d, d) > rr * rr)
								continue;

							// Narrowphase: circle vs concave triangle soup
							var triRef = tTriRefs[ti].Value;
							if (!triRef.IsCreated)
								continue;

							bool hit = Narrowphase2D.CircleIntersectsTriangleSoupWorld(
								bp,
								br,
								ref triRef.Value.Vertices,
								tPoses[ti].Value.Position,
								tPoses[ti].Value.Theta); // degrees

							if (!hit)
								continue;

							hitTarget = tEntities[ti];

							// Damage example
							if (em.HasComponent<Health>(hitTarget))
							{
								var h = em.GetComponentData<Health>(hitTarget);
								h.Value -= bDmgRO.ValueRO.Value;
								em.SetComponentData(hitTarget, h);
							}

							break;
						}
					}

				if (hitTarget != Entity.Null)
				{
					ecb.DestroyEntity(bEntity);
				}
			}

			ecb.Playback(em);
			ecb.Dispose();
			grid.Dispose();
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int Hash(int2 cell) => (cell.x * 73856093) ^ (cell.y * 19349663);
	}

	#endregion
}
