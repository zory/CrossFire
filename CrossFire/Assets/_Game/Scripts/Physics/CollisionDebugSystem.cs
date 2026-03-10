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

			using EntityQuery allColliderQuery = entityManager.CreateEntityQuery(
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

			using NativeArray<Entity> allEntities = allColliderQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<WorldPose> allPoses = allColliderQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using NativeArray<CollisionLayer> allLayers = allColliderQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using NativeArray<CollisionMask> allMasks = allColliderQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using NativeArray<Collider2D> allColliders = allColliderQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);

			using NativeParallelMultiHashMap<int, int> concaveGrid =
				new NativeParallelMultiHashMap<int, int>(math.max(1, concaveEntities.Length * 2), Allocator.Temp);

			BuildConcaveGrid(
				concaveGrid,
				concavePoses,
				concaveColliders,
				inverseCellSize);

			DrawColliderShapes(
				concavePoses,
				concaveColliders,
				concaveTriangleRefs,
				allPoses,
				allColliders);

			DrawBroadphaseAndHitTriangles(
				concaveGrid,
				inverseCellSize,
				concavePoses,
				concaveLayers,
				concaveMasks,
				concaveColliders,
				concaveTriangleRefs,
				allEntities,
				allPoses,
				allLayers,
				allMasks,
				allColliders);
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
			in NativeArray<Collider2D> concaveColliders,
			float inverseCellSize)
		{
			for (int concaveIndex = 0; concaveIndex < concavePoses.Length; concaveIndex++)
			{
				if (concaveColliders[concaveIndex].Type != Collider2DType.ConcaveTriangles)
				{
					continue;
				}

				float2 concavePositionWorld = concavePoses[concaveIndex].Value.Position;
				int2 cell = (int2)math.floor(concavePositionWorld * inverseCellSize);
				concaveGrid.Add(Hash(cell), concaveIndex);
			}
		}

		private static void DrawColliderShapes(
			in NativeArray<WorldPose> concavePoses,
			in NativeArray<Collider2D> concaveColliders,
			in NativeArray<ConcaveTrianglesRef> concaveTriangleRefs,
			in NativeArray<WorldPose> allPoses,
			in NativeArray<Collider2D> allColliders)
		{
			for (int colliderIndex = 0; colliderIndex < allColliders.Length; colliderIndex++)
			{
				Collider2D collider = allColliders[colliderIndex];
				float2 positionWorld = allPoses[colliderIndex].Value.Position;

				if (collider.Type == Collider2DType.Circle)
				{
					float circleRadius = math.max(0f, collider.CircleRadius);

					DrawCircle(
						positionWorld,
						circleRadius,
						Color.cyan,
						CollisionDebugSettings.CircleSegments,
						CollisionDebugSettings.ZOffset);
				}
			}

			for (int concaveIndex = 0; concaveIndex < concaveColliders.Length; concaveIndex++)
			{
				if (concaveColliders[concaveIndex].Type != Collider2DType.ConcaveTriangles)
				{
					continue;
				}

				float2 positionWorld = concavePoses[concaveIndex].Value.Position;
				float rotationRadians = concavePoses[concaveIndex].Value.ThetaRad;

				float boundRadius = math.max(0f, concaveColliders[concaveIndex].BoundRadius);
				DrawCircle(
					positionWorld,
					boundRadius,
					Color.yellow,
					CollisionDebugSettings.CircleSegments,
					CollisionDebugSettings.ZOffset);

				BlobAssetReference<TriangleSoupBlob> triangleBlob = concaveTriangleRefs[concaveIndex].Value;
				if (!triangleBlob.IsCreated)
				{
					continue;
				}

				DrawTriangleSoupWorld(
					ref triangleBlob.Value.Vertices,
					positionWorld,
					rotationRadians,
					Color.green,
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
			in NativeArray<Entity> allEntities,
			in NativeArray<WorldPose> allPoses,
			in NativeArray<CollisionLayer> allLayers,
			in NativeArray<CollisionMask> allMasks,
			in NativeArray<Collider2D> allColliders)
		{
			for (int colliderIndex = 0; colliderIndex < allEntities.Length; colliderIndex++)
			{
				Collider2D collider = allColliders[colliderIndex];
				if (collider.Type != Collider2DType.Circle)
				{
					continue;
				}

				float2 circleCenterWorld = allPoses[colliderIndex].Value.Position;
				float circleRadius = math.max(0f, collider.CircleRadius);
				CollisionLayer circleLayer = allLayers[colliderIndex];
				CollisionMask circleMask = allMasks[colliderIndex];

				int2 baseCell = (int2)math.floor(circleCenterWorld * inverseCellSize);

				for (int yOffset = -1; yOffset <= 1; yOffset++)
				{
					for (int xOffset = -1; xOffset <= 1; xOffset++)
					{
						int2 candidateCell = baseCell + new int2(xOffset, yOffset);
						int cellHash = Hash(candidateCell);

						NativeParallelMultiHashMapIterator<int> iterator;
						int concaveIndex;

						bool found = concaveGrid.TryGetFirstValue(
							cellHash,
							out concaveIndex,
							out iterator);

						while (found)
						{
							if (concaveColliders[concaveIndex].Type != Collider2DType.ConcaveTriangles)
							{
								found = concaveGrid.TryGetNextValue(out concaveIndex, ref iterator);
								continue;
							}

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

		private static bool TryFindHitTriangle(
			float2 circleCenterWorld,
			float circleRadius,
			ref BlobArray<float2> trianglesLocal,
			float2 shapePositionWorld,
			float shapeRotationRadians,
			out int hitTriangleStartIndex)
		{
			hitTriangleStartIndex = -1;

			float rotationCosine = math.cos(shapeRotationRadians);
			float rotationSine = math.sin(shapeRotationRadians);
			float circleRadiusSquared = circleRadius * circleRadius;

			for (int triangleStartIndex = 0;
				 triangleStartIndex + 2 < trianglesLocal.Length;
				 triangleStartIndex += 3)
			{
				float2 a = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 0], rotationCosine, rotationSine) + shapePositionWorld;
				float2 b = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 1], rotationCosine, rotationSine) + shapePositionWorld;
				float2 c = PhysicsUtilities.Rotate(trianglesLocal[triangleStartIndex + 2], rotationCosine, rotationSine) + shapePositionWorld;

				if (CircleIntersectsTriangleWorld(circleCenterWorld, circleRadiusSquared, a, b, c))
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
			float rotationRadians,
			Color color,
			float z)
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

				Debug.DrawLine(ToV3(a, z), ToV3(b, z), color);
				Debug.DrawLine(ToV3(b, z), ToV3(c, z), color);
				Debug.DrawLine(ToV3(c, z), ToV3(a, z), color);
			}
		}

		private static void DrawSingleTriangleWorld(
			ref BlobArray<float2> trianglesLocal,
			int startIndex,
			float2 positionWorld,
			float rotationRadians,
			Color color,
			float z)
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


		private static bool CircleIntersectsTriangleWorld(
			float2 circleCenterWorld,
			float circleRadiusSquared,
			float2 a,
			float2 b,
			float2 c)
		{
			if (PhysicsUtilities.PointInTriangle(circleCenterWorld, a, b, c))
			{
				return true;
			}

			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, a, b) <= circleRadiusSquared) return true;
			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, b, c) <= circleRadiusSquared) return true;
			if (PhysicsUtilities.DistanceSquaredPointToSegment(circleCenterWorld, c, a) <= circleRadiusSquared) return true;

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