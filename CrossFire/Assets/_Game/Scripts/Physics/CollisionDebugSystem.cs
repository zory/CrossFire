using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
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

			using EntityQuery gridQuery =
				entityManager.CreateEntityQuery(ComponentType.ReadOnly<CollisionGridSettings>());

			if (gridQuery.IsEmptyIgnoreFilter)
			{
				return;
			}

			DrawCollisionDebug(entityManager);
#endif
		}

#if UNITY_EDITOR
		private static void DrawCollisionDebug(EntityManager entityManager)
		{
			float inverseCellSize = GetInverseCellSize(entityManager);

			using EntityQuery concaveColliderQuery = entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<CollisionLayer>(),
				ComponentType.ReadOnly<CollisionMask>(),
				ComponentType.ReadOnly<Collider2D>(),
				ComponentType.ReadOnly<ConcaveTrianglesRef>());

			using EntityQuery circleColliderQuery = entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<CollisionLayer>(),
				ComponentType.ReadOnly<CollisionMask>(),
				ComponentType.ReadOnly<Collider2D>());

			using NativeArray<Entity> concaveEntities = concaveColliderQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<WorldPose> concavePoses = concaveColliderQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using NativeArray<CollisionLayer> concaveLayers = concaveColliderQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using NativeArray<CollisionMask> concaveMasks = concaveColliderQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using NativeArray<Collider2D> concaveColliders = concaveColliderQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);
			using NativeArray<ConcaveTrianglesRef> concaveTriangleRefs =
				concaveColliderQuery.ToComponentDataArray<ConcaveTrianglesRef>(Allocator.Temp);

			using NativeArray<Entity> circleEntities = circleColliderQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<WorldPose> circlePoses = circleColliderQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using NativeArray<CollisionLayer> circleLayers = circleColliderQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using NativeArray<CollisionMask> circleMasks = circleColliderQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using NativeArray<Collider2D> circleColliders = circleColliderQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);

			using NativeParallelMultiHashMap<int, int> concaveGrid =
				new NativeParallelMultiHashMap<int, int>(math.max(1, concaveEntities.Length * 2), Allocator.Temp);

			BuildConcaveGrid(
				in concaveGrid,
				concavePoses,
				inverseCellSize);

			DrawAllColliders(
				concavePoses,
				concaveColliders,
				concaveTriangleRefs,
				circlePoses,
				circleColliders);

			DrawBroadphaseAndHitTriangles(
				in concaveGrid,
				inverseCellSize,
				concavePoses,
				concaveLayers,
				concaveMasks,
				concaveColliders,
				concaveTriangleRefs,
				circleEntities,
				circlePoses,
				circleLayers,
				circleMasks,
				circleColliders);
		}

		private static float GetInverseCellSize(EntityManager entityManager)
		{
			using EntityQuery gridQuery =
				entityManager.CreateEntityQuery(ComponentType.ReadOnly<CollisionGridSettings>());

			if (gridQuery.IsEmptyIgnoreFilter)
			{
				return 1f;
			}

			CollisionGridSettings collisionGridSettings = gridQuery.GetSingleton<CollisionGridSettings>();
			float cellSize = math.max(0.0001f, collisionGridSettings.CellSize);
			return 1f / cellSize;
		}

		private static void BuildConcaveGrid(
			in NativeParallelMultiHashMap<int, int> concaveGrid,
			in NativeArray<WorldPose> concavePoses,
			float inverseCellSize)
		{
			for (int concaveIndex = 0; concaveIndex < concavePoses.Length; concaveIndex++)
			{
				float2 concavePositionWorld = concavePoses[concaveIndex].Value.Position;
				int2 cell = (int2)math.floor(concavePositionWorld * inverseCellSize);
				concaveGrid.Add(Hash(cell), concaveIndex);
			}
		}

		private static void DrawAllColliders(
			in NativeArray<WorldPose> concavePoses,
			in NativeArray<Collider2D> concaveColliders,
			in NativeArray<ConcaveTrianglesRef> concaveTriangleRefs,
			in NativeArray<WorldPose> circlePoses,
			in NativeArray<Collider2D> circleColliders)
		{
			for (int concaveIndex = 0; concaveIndex < concavePoses.Length; concaveIndex++)
			{
				float2 positionWorld = concavePoses[concaveIndex].Value.Position;
				float thetaRad = concavePoses[concaveIndex].Value.ThetaRad;
				float boundRadius = math.max(0f, concaveColliders[concaveIndex].BoundRadius);

				DrawCircle(
					positionWorld,
					boundRadius,
					Color.yellow,
					CollisionDebugSettings.CircleSegments,
					CollisionDebugSettings.ZOffset);

				BlobAssetReference<TriangleSoupBlob> triangleBlob = concaveTriangleRefs[concaveIndex].Value;
				if (triangleBlob.IsCreated)
				{
					DrawTriangleSoupWorld(
						ref triangleBlob.Value.Vertices,
						positionWorld,
						thetaRad,
						Color.green,
						CollisionDebugSettings.ZOffset);
				}
			}

			for (int circleIndex = 0; circleIndex < circlePoses.Length; circleIndex++)
			{
				float2 positionWorld = circlePoses[circleIndex].Value.Position;
				float radius = GetCircleDebugRadius(circleColliders[circleIndex]);

				DrawCircle(
					positionWorld,
					radius,
					Color.cyan,
					CollisionDebugSettings.CircleSegments,
					CollisionDebugSettings.ZOffset);
			}
		}

		private static void DrawBroadphaseAndHitTriangles(
			in NativeParallelMultiHashMap<int, int> concaveGrid,
			float inverseCellSize,
			in NativeArray<WorldPose> concavePoses,
			in NativeArray<CollisionLayer> concaveLayers,
			in NativeArray<CollisionMask> concaveMasks,
			in NativeArray<Collider2D> concaveColliders,
			in NativeArray<ConcaveTrianglesRef> concaveTriangleRefs,
			in NativeArray<Entity> circleEntities,
			in NativeArray<WorldPose> circlePoses,
			in NativeArray<CollisionLayer> circleLayers,
			in NativeArray<CollisionMask> circleMasks,
			in NativeArray<Collider2D> circleColliders)
		{
			for (int circleIndex = 0; circleIndex < circleEntities.Length; circleIndex++)
			{
				float2 circleCenterWorld = circlePoses[circleIndex].Value.Position;
				float circleRadius = GetCircleDebugRadius(circleColliders[circleIndex]);
				CollisionLayer circleLayer = circleLayers[circleIndex];
				CollisionMask circleMask = circleMasks[circleIndex];

				int2 baseCell = (int2)math.floor(circleCenterWorld * inverseCellSize);

				for (int yOffset = -1; yOffset <= 1; yOffset++)
				{
					for (int xOffset = -1; xOffset <= 1; xOffset++)
					{
						int2 candidateCell = baseCell + new int2(xOffset, yOffset);
						int cellHash = Hash(candidateCell);

						NativeParallelMultiHashMapIterator<int> iterator;
						int concaveIndex;

						bool found =
							concaveGrid.TryGetFirstValue(
								cellHash,
								out concaveIndex,
								out iterator);

						while (found)
						{
							if (!PhysicsUtilities.CanCollide(
									circleLayer,
									circleMask,
									concaveLayers[concaveIndex],
									concaveMasks[concaveIndex]))
							{
								found = concaveGrid.TryGetNextValue(out concaveIndex, ref iterator);
								continue;
							}

							float2 concavePositionWorld = concavePoses[concaveIndex].Value.Position;
							float concaveBoundRadius = math.max(0f, concaveColliders[concaveIndex].BoundRadius);

							float2 delta = concavePositionWorld - circleCenterWorld;
							float combinedRadius = concaveBoundRadius + circleRadius;
							bool broadphasePass = math.dot(delta, delta) <= combinedRadius * combinedRadius;

							if (CollisionDebugSettings.DrawBroadphase && broadphasePass)
							{
								Debug.DrawLine(
									ToV3(circleCenterWorld, CollisionDebugSettings.ZOffset),
									ToV3(concavePositionWorld, CollisionDebugSettings.ZOffset),
									new Color(1f, 0.5f, 0f, 1f));
							}

							if (!broadphasePass)
							{
								found = concaveGrid.TryGetNextValue(out concaveIndex, ref iterator);
								continue;
							}

							BlobAssetReference<TriangleSoupBlob> triangleBlob = concaveTriangleRefs[concaveIndex].Value;
							if (!triangleBlob.IsCreated)
							{
								found = concaveGrid.TryGetNextValue(out concaveIndex, ref iterator);
								continue;
							}

							int hitTriangleStartIndex;
							bool narrowphaseHit = TryFindHitTriangle(
								circleCenterWorld,
								circleRadius,
								ref triangleBlob.Value.Vertices,
								concavePositionWorld,
								concavePoses[concaveIndex].Value.ThetaRad,
								out hitTriangleStartIndex);

							if (narrowphaseHit &&
								CollisionDebugSettings.DrawHitTriangles &&
								hitTriangleStartIndex >= 0)
							{
								DrawSingleTriangleWorld(
									ref triangleBlob.Value.Vertices,
									hitTriangleStartIndex,
									concavePositionWorld,
									concavePoses[concaveIndex].Value.ThetaRad,
									Color.red,
									CollisionDebugSettings.ZOffset);
							}

							found = concaveGrid.TryGetNextValue(out concaveIndex, ref iterator);
						}
					}
				}
			}
		}

		private static float GetCircleDebugRadius(in Collider2D collider)
		{
			float radius = math.max(0f, collider.CircleRadius);
			if (radius <= 0f)
			{
				radius = math.max(0f, collider.BoundRadius);
			}

			return radius;
		}

		private static bool TryFindHitTriangle(
			float2 circleCenterWorld,
			float circleRadius,
			ref BlobArray<float2> trianglesLocal,
			float2 shapePositionWorld,
			float shapeRotationRad,
			out int hitTriangleStartIndex)
		{
			hitTriangleStartIndex = -1;

			float cosine = math.cos(shapeRotationRad);
			float sine = math.sin(shapeRotationRad);
			float radiusSquared = circleRadius * circleRadius;

			for (int triangleStartIndex = 0;
				 triangleStartIndex + 2 < trianglesLocal.Length;
				 triangleStartIndex += 3)
			{
				float2 a = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 0], cosine, sine) + shapePositionWorld;
				float2 b = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 1], cosine, sine) + shapePositionWorld;
				float2 c = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 2], cosine, sine) + shapePositionWorld;

				if (CircleIntersectsTriangleWorld(circleCenterWorld, radiusSquared, a, b, c))
				{
					hitTriangleStartIndex = triangleStartIndex;
					return true;
				}
			}

			return false;
		}

		private static void DrawTriangleSoupWorld(
			ref BlobArray<float2> trianglesLocal,
			float2 positionWorld,
			float rotationRad,
			Color color,
			float z)
		{
			float cosine = math.cos(rotationRad);
			float sine = math.sin(rotationRad);

			for (int triangleStartIndex = 0;
				 triangleStartIndex + 2 < trianglesLocal.Length;
				 triangleStartIndex += 3)
			{
				float2 a = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 0], cosine, sine) + positionWorld;
				float2 b = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 1], cosine, sine) + positionWorld;
				float2 c = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 2], cosine, sine) + positionWorld;

				Debug.DrawLine(ToV3(a, z), ToV3(b, z), color);
				Debug.DrawLine(ToV3(b, z), ToV3(c, z), color);
				Debug.DrawLine(ToV3(c, z), ToV3(a, z), color);
			}
		}

		private static void DrawSingleTriangleWorld(
			ref BlobArray<float2> trianglesLocal,
			int startIndex,
			float2 positionWorld,
			float rotationRad,
			Color color,
			float z)
		{
			if (startIndex < 0 || startIndex + 2 >= trianglesLocal.Length)
			{
				return;
			}

			float cosine = math.cos(rotationRad);
			float sine = math.sin(rotationRad);

			float2 a = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 0], cosine, sine) + positionWorld;
			float2 b = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 1], cosine, sine) + positionWorld;
			float2 c = PhysicsUtilities.Rotate(trianglesLocal[startIndex + 2], cosine, sine) + positionWorld;

			Debug.DrawLine(ToV3(a, z), ToV3(b, z), color);
			Debug.DrawLine(ToV3(b, z), ToV3(c, z), color);
			Debug.DrawLine(ToV3(c, z), ToV3(a, z), color);
		}

		private static void DrawCircle(float2 center, float radius, Color color, int segments, float z)
		{
			if (radius <= 0f || segments < 3)
			{
				return;
			}

			float step = math.PI * 2f / segments;
			float2 previousPoint = center + new float2(math.cos(0f), math.sin(0f)) * radius;

			for (int index = 1; index <= segments; index++)
			{
				float angle = index * step;
				float2 nextPoint = center + new float2(math.cos(angle), math.sin(angle)) * radius;

				Debug.DrawLine(ToV3(previousPoint, z), ToV3(nextPoint, z), color);
				previousPoint = nextPoint;
			}
		}

		private static bool CircleIntersectsTriangleWorld(float2 point, float radiusSquared, float2 a, float2 b, float2 c)
		{
			if (PhysicsUtilities.PointInTriangle(point, a, b, c))
			{
				return true;
			}

			if (PhysicsUtilities.DistanceSquaredPointToSegment(point, a, b) <= radiusSquared) return true;
			if (PhysicsUtilities.DistanceSquaredPointToSegment(point, b, c) <= radiusSquared) return true;
			if (PhysicsUtilities.DistanceSquaredPointToSegment(point, c, a) <= radiusSquared) return true;

			return false;
		}





		private static Vector3 ToV3(float2 point, float z)
		{
			return new Vector3(point.x, point.y, z);
		}

		private static int Hash(int2 cell)
		{
			return (cell.x * 73856093) ^ (cell.y * 19349663);
		}
#endif
	}
}