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
}