using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Physics
{
	public struct PrevWorldPose : IComponentData { public Pose2D Value; }
	public struct WorldPose : IComponentData { public Pose2D Value; }
	public struct Velocity : IComponentData { public float2 Value; }
	public struct AngularVelocity : IComponentData
	{
		// radians per second
		public float Value;
	}

	public struct LinearDamping : IComponentData
	{
		public float Value; // per second
	}

	public struct MaxVelocity : IComponentData { public float Value; }



	public struct CollisionGridSettings : IComponentData
	{
		public float CellSize; // e.g. 2..8 world units depending on density/ship size
	}

	public struct CollisionLayer : IComponentData { public uint Value; } // one-hot

	public struct CollisionMask : IComponentData { public uint Value; } // bitset

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

	public struct CollisionEvent : IBufferElementData
	{
		public Entity FirstEntity;
		public Entity SecondEntity;
	}

	public struct CollisionEventBufferTag : IComponentData
	{
	}
}