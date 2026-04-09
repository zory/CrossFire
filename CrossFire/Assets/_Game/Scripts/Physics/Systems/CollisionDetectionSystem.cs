using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	/// <summary>
	/// Broadphase grid + narrowphase intersection tests for all entities carrying a
	/// <see cref="Collider2D"/>. Detected contacts are written as <see cref="CollisionEvent"/>
	/// entries to the singleton <see cref="CollisionEventBufferTag"/> buffer, which is cleared
	/// at the start of every update so downstream systems always see only the current frame's
	/// contacts.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Physics — runs after all integration systems have committed final world
	/// positions and before combat-reaction systems read the collision buffer.
	/// Requires a <see cref="CollisionGridSettings"/> singleton for broadphase cell sizing and
	/// a <see cref="CollisionEventBufferTag"/> singleton as the output buffer.
	/// The system intentionally runs even when no collider entities are present so the buffer
	/// is always cleared and downstream systems never observe stale events.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct CollisionDetectionSystem : ISystem
	{
		private EntityQuery _colliderQuery;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_colliderQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, CollisionLayer, CollisionMask, Collider2D>()
				.Build(ref state);

			state.RequireForUpdate<CollisionGridSettings>();
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			//Get singleton settings
			CollisionGridSettings collisionGridSettings = SystemAPI.GetSingleton<CollisionGridSettings>();
			float cellSize = math.max(0.0001f, collisionGridSettings.CellSize);
			float inverseCellSize = 1f / cellSize;

			Entity collisionEventBufferEntity = SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();
			DynamicBuffer<CollisionEvent> collisionEventBuffer = entityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);
			collisionEventBuffer.Clear();

			//Allocate temporary containers / Collect entity queries
			using NativeArray<Entity> colliderEntities = _colliderQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<WorldPose> colliderWorldPoses = _colliderQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using NativeArray<CollisionLayer> colliderCollisionLayers = _colliderQuery.ToComponentDataArray<CollisionLayer>(Allocator.Temp);
			using NativeArray<CollisionMask> colliderCollisionMasks = _colliderQuery.ToComponentDataArray<CollisionMask>(Allocator.Temp);
			using NativeArray<Collider2D> colliders = _colliderQuery.ToComponentDataArray<Collider2D>(Allocator.Temp);

			// NativeArray and NativeParallelMultiHashMap/HashSet are mutated after creation,
			// so they cannot use the `using` declaration (which makes the variable readonly).
			// They are disposed explicitly at the end of OnUpdate.
			NativeArray<ConcaveTrianglesRef> colliderTriangleReferences =
				new NativeArray<ConcaveTrianglesRef>(
					colliderEntities.Length,
					Allocator.Temp,
					NativeArrayOptions.ClearMemory);
			for (int colliderIndex = 0; colliderIndex < colliderEntities.Length; colliderIndex++)
			{
				Entity colliderEntity = colliderEntities[colliderIndex];
				if (entityManager.HasComponent<ConcaveTrianglesRef>(colliderEntity))
				{
					colliderTriangleReferences[colliderIndex] = entityManager.GetComponentData<ConcaveTrianglesRef>(colliderEntity);
				}
			}

			//Build spatial grid
			NativeParallelMultiHashMap<int, int> colliderGrid =
				new NativeParallelMultiHashMap<int, int>(
					math.max(1, colliderEntities.Length * 4),
					Allocator.Temp);
			for (int colliderIndex = 0; colliderIndex < colliderEntities.Length; colliderIndex++)
			{
				float2 colliderPositionWorld = colliderWorldPoses[colliderIndex].Value.Position;
				float colliderBoundRadius = math.max(0f, colliders[colliderIndex].BoundRadius);
				float2 minimumWorld = colliderPositionWorld - new float2(colliderBoundRadius, colliderBoundRadius);
				float2 maximumWorld = colliderPositionWorld + new float2(colliderBoundRadius, colliderBoundRadius);
				int2 minimumCellCoordinates = (int2)math.floor(minimumWorld * inverseCellSize);
				int2 maximumCellCoordinates = (int2)math.floor(maximumWorld * inverseCellSize);
				for (int cellY = minimumCellCoordinates.y; cellY <= maximumCellCoordinates.y; cellY++)
				{
					for (int cellX = minimumCellCoordinates.x; cellX <= maximumCellCoordinates.x; cellX++)
					{
						int2 cellCoordinates = new int2(cellX, cellY);
						int cellHash = Hash(cellCoordinates);
						colliderGrid.Add(cellHash, colliderIndex);
					}
				}
			}

			NativeParallelHashSet<ulong> emittedCollisionPairs =
				new NativeParallelHashSet<ulong>(
					math.max(1, colliderEntities.Length * 4),
					Allocator.Temp);

			//Broadphase search 3×3 neighboring cells
			for (int firstColliderIndex = 0; firstColliderIndex < colliderEntities.Length; firstColliderIndex++)
			{
				float2 firstColliderPositionWorld = colliderWorldPoses[firstColliderIndex].Value.Position;
				float firstColliderBoundRadius = math.max(0f, colliders[firstColliderIndex].BoundRadius);

				float2 minimumWorld = firstColliderPositionWorld - new float2(firstColliderBoundRadius, firstColliderBoundRadius);
				float2 maximumWorld = firstColliderPositionWorld + new float2(firstColliderBoundRadius, firstColliderBoundRadius);

				int2 minimumCellCoordinates = (int2)math.floor(minimumWorld * inverseCellSize);
				int2 maximumCellCoordinates = (int2)math.floor(maximumWorld * inverseCellSize);

				for (int cellY = minimumCellCoordinates.y; cellY <= maximumCellCoordinates.y; cellY++)
				{
					for (int cellX = minimumCellCoordinates.x; cellX <= maximumCellCoordinates.x; cellX++)
					{
						int2 cellCoordinates = new int2(cellX, cellY);
						int cellHash = Hash(cellCoordinates);

						NativeParallelMultiHashMapIterator<int> colliderGridIterator;
						int secondColliderIndex;

						bool foundAnyColliderInCell =
							colliderGrid.TryGetFirstValue(
								cellHash,
								out secondColliderIndex,
								out colliderGridIterator);

						while (foundAnyColliderInCell)
						{
							if (secondColliderIndex != firstColliderIndex)
							{
								int lowerColliderIndex = math.min(firstColliderIndex, secondColliderIndex);
								int higherColliderIndex = math.max(firstColliderIndex, secondColliderIndex);

								ulong collisionPairKey = MakePairKey(lowerColliderIndex, higherColliderIndex);
								bool pairWasAlreadyProcessed = emittedCollisionPairs.Contains(collisionPairKey);

								//Collision filtering - Skip pairs that cannot collide
								if (!pairWasAlreadyProcessed)
								{
									emittedCollisionPairs.Add(collisionPairKey);

									CollisionLayer firstCollisionLayer = colliderCollisionLayers[firstColliderIndex];
									CollisionMask firstCollisionMask = colliderCollisionMasks[firstColliderIndex];

									CollisionLayer secondCollisionLayer = colliderCollisionLayers[secondColliderIndex];
									CollisionMask secondCollisionMask = colliderCollisionMasks[secondColliderIndex];

									bool collisionFilterPassed = PhysicsUtilities.CanCollide(
										firstCollisionLayer, firstCollisionMask,
										secondCollisionLayer, secondCollisionMask);

									if (collisionFilterPassed)
									{
										float2 secondColliderPositionWorld = colliderWorldPoses[secondColliderIndex].Value.Position;
										float secondColliderBoundRadius = math.max(0f, colliders[secondColliderIndex].BoundRadius);

										float2 deltaBetweenColliders = secondColliderPositionWorld - firstColliderPositionWorld;
										float combinedBoundRadius = firstColliderBoundRadius + secondColliderBoundRadius;

										float combinedBoundRadiusSquared = combinedBoundRadius * combinedBoundRadius;
										float distanceSquaredBetweenColliders = math.dot(deltaBetweenColliders, deltaBetweenColliders);

										bool broadPhasePassed = distanceSquaredBetweenColliders <= combinedBoundRadiusSquared;
										if (broadPhasePassed)
										{
											//Narrowphase
											bool narrowPhasePassed =
												GeometryUtilities.Intersects(
													colliders[firstColliderIndex],
													colliderWorldPoses[firstColliderIndex],
													colliderTriangleReferences[firstColliderIndex],
													colliders[secondColliderIndex],
													colliderWorldPoses[secondColliderIndex],
													colliderTriangleReferences[secondColliderIndex]);

											if (narrowPhasePassed)
											{
												//Write collision to collision buffer
												CollisionEvent collisionEvent =
													new CollisionEvent()
													{
														FirstEntity = colliderEntities[firstColliderIndex],
														SecondEntity = colliderEntities[secondColliderIndex]
													};
												collisionEventBuffer.Add(collisionEvent);
											}
										}
									}
								}
							}

							foundAnyColliderInCell =
								colliderGrid.TryGetNextValue(
									out secondColliderIndex,
									ref colliderGridIterator);
						}
					}
				}
			}

			colliderTriangleReferences.Dispose();
			emittedCollisionPairs.Dispose();
			colliderGrid.Dispose();
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int Hash(int2 cellCoordinates)
		{
			return (cellCoordinates.x * 73856093) ^ (cellCoordinates.y * 19349663);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static ulong MakePairKey(
			int lowerColliderIndex,
			int higherColliderIndex)
		{
			uint lowerColliderIndexUnsigned = (uint)lowerColliderIndex;
			uint higherColliderIndexUnsigned = (uint)higherColliderIndex;
			ulong pairKey = ((ulong)lowerColliderIndexUnsigned << 32) | higherColliderIndexUnsigned;

			return pairKey;
		}
	}
}
