using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Factory helpers for creating ECS entities with common Core.Physics component
	/// combinations. Keeps individual test methods free of repetitive AddComponentData calls.
	/// </summary>
	public static class PhysicsEntityFactory
	{
		/// <summary>
		/// Entity with WorldPose and PrevWorldPose. Required by SnapshotSystem and all
		/// downstream systems that read WorldPose.
		/// </summary>
		public static Entity CreateEntityWithPose(EntityManager entityManager, Pose2D pose)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new WorldPose { Value = pose });
			entityManager.AddComponentData(entity, new PrevWorldPose { Value = default });
			return entity;
		}

		/// <summary>
		/// Entity with only a Velocity component. Used by systems that operate on
		/// velocity alone (e.g. LinearDampingSystem, MaxVelocityClampSystem).
		/// </summary>
		public static Entity CreateEntityWithVelocity(EntityManager entityManager, float2 velocity)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new Velocity { Value = velocity });
			return entity;
		}

		/// <summary>
		/// Entity with WorldPose, PrevWorldPose, Velocity and AngularVelocity.
		/// The standard moving body used by integration systems.
		/// </summary>
		public static Entity CreateDynamicBody(
			EntityManager entityManager,
			Pose2D pose,
			float2 velocity,
			float angularVelocity = 0f)
		{
			Entity entity = CreateEntityWithPose(entityManager, pose);
			entityManager.AddComponentData(entity, new Velocity { Value = velocity });
			entityManager.AddComponentData(entity, new AngularVelocity { Value = angularVelocity });
			return entity;
		}

		/// <summary>
		/// Entity with WorldPose, PrevWorldPose, and an identity LocalTransform.
		/// Used by PostPhysicsSystem tests, which sync WorldPose into LocalTransform.
		/// </summary>
		public static Entity CreateEntityWithPoseAndTransform(EntityManager entityManager, Pose2D pose)
		{
			Entity entity = CreateEntityWithPose(entityManager, pose);
			entityManager.AddComponentData(entity, LocalTransform.Identity);
			return entity;
		}

		/// <summary>
		/// Adds LinearDamping to an existing entity.
		/// </summary>
		public static void AddLinearDamping(EntityManager entityManager, Entity entity, float damping)
		{
			entityManager.AddComponentData(entity, new LinearDamping { Value = damping });
		}

		/// <summary>
		/// Adds MaxVelocity to an existing entity.
		/// </summary>
		public static void AddMaxVelocity(EntityManager entityManager, Entity entity, float maxVelocity)
		{
			entityManager.AddComponentData(entity, new MaxVelocity { Value = maxVelocity });
		}

		/// <summary>
		/// Creates the two singletons required by CollisionDetectionSystem:
		/// a <see cref="CollisionGridSettings"/> entity and a <see cref="CollisionEventBufferTag"/>
		/// entity with an empty <see cref="CollisionEvent"/> buffer.
		/// Returns the buffer entity so tests can read collision results from it.
		/// </summary>
		public static Entity CreateCollisionSingletons(EntityManager entityManager, float cellSize)
		{
			Entity settingsEntity = entityManager.CreateEntity();
			entityManager.AddComponentData(settingsEntity, new CollisionGridSettings { CellSize = cellSize });

			Entity bufferEntity = entityManager.CreateEntity();
			entityManager.AddComponentData(bufferEntity, new CollisionEventBufferTag());
			entityManager.AddBuffer<CollisionEvent>(bufferEntity);

			return bufferEntity;
		}

		/// <summary>
		/// Creates a circle collider entity with WorldPose, CollisionLayer, CollisionMask, and
		/// Collider2D. BoundRadius equals CircleRadius, which is the correct setup for circle
		/// vs circle broadphase.
		/// </summary>
		public static Entity CreateCircleColliderEntity(
			EntityManager entityManager,
			float2 position,
			float circleRadius,
			uint layer,
			uint mask)
		{
			return CreateCircleColliderEntity(entityManager, position, circleRadius, circleRadius, layer, mask);
		}

		/// <summary>
		/// Creates a circle collider entity with an explicit BoundRadius separate from
		/// CircleRadius. Use this overload when testing broadphase vs narrowphase disagreement
		/// (e.g. BoundRadius large enough to enter the grid but CircleRadius too small to
		/// actually touch the other collider).
		/// </summary>
		public static Entity CreateCircleColliderEntity(
			EntityManager entityManager,
			float2 position,
			float circleRadius,
			float boundRadius,
			uint layer,
			uint mask)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new WorldPose { Value = new Pose2D { Position = position, ThetaRad = 0f } });
			entityManager.AddComponentData(entity, new CollisionLayer { Value = layer });
			entityManager.AddComponentData(entity, new CollisionMask { Value = mask });
			entityManager.AddComponentData(entity, new Collider2D
			{
				Type = Collider2DType.Circle,
				CircleRadius = circleRadius,
				BoundRadius = boundRadius
			});
			return entity;
		}

		/// <summary>
		/// Builds a <see cref="BlobAssetReference{TriangleSoupBlob}"/> for a single triangle.
		/// The caller owns the returned reference and must dispose it after the test completes.
		/// </summary>
		public static BlobAssetReference<TriangleSoupBlob> CreateSingleTriangleBlob(float2 a, float2 b, float2 c)
		{
			BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
			ref TriangleSoupBlob blob = ref blobBuilder.ConstructRoot<TriangleSoupBlob>();
			BlobBuilderArray<float2> vertices = blobBuilder.Allocate(ref blob.Vertices, 3);
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
			BlobAssetReference<TriangleSoupBlob> blobRef =
				blobBuilder.CreateBlobAssetReference<TriangleSoupBlob>(Allocator.Persistent);
			blobBuilder.Dispose();
			return blobRef;
		}

		/// <summary>
		/// Creates a ConcaveTriangles collider entity.
		/// The <paramref name="triangleBlob"/> must remain alive until after the final
		/// world update in the test; the caller is responsible for disposing it.
		/// </summary>
		public static Entity CreateTriangleColliderEntity(
			EntityManager entityManager,
			float2 position,
			float boundRadius,
			BlobAssetReference<TriangleSoupBlob> triangleBlob,
			uint layer,
			uint mask)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new WorldPose { Value = new Pose2D { Position = position, ThetaRad = 0f } });
			entityManager.AddComponentData(entity, new CollisionLayer { Value = layer });
			entityManager.AddComponentData(entity, new CollisionMask { Value = mask });
			entityManager.AddComponentData(entity, new Collider2D
			{
				Type = Collider2DType.ConcaveTriangles,
				CircleRadius = 0f,
				BoundRadius = boundRadius
			});
			entityManager.AddComponentData(entity, new ConcaveTrianglesRef { Value = triangleBlob });
			return entity;
		}
	}
}
