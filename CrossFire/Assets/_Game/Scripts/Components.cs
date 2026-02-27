using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire
{
	public struct StableId : IComponentData { public int Value; }

	public struct ControlledTag : IComponentData { }

	public struct SelectableTag : IComponentData { }

	public struct ShipTag : IComponentData { }

	public struct TeamId : IComponentData { public byte Value; }

	public struct NativeColor : IComponentData { public float4 Value; }

	public struct PrevWorldPose : IComponentData { public Pose2D Value; }

	public struct WorldPose : IComponentData { public Pose2D Value; }

	public struct Velocity : IComponentData { public float2 Value; }

	public struct MaxSpeed : IComponentData { public float Value; }

	public struct TurnSpeed : IComponentData
	{
		// radians per second
		public float Value;
	}

	public struct ThrustAcceleration : IComponentData { public float Value; }

	public struct BrakeAcceleleration : IComponentData { public float Value; }

	public struct CollisionRadius : IComponentData { public float Value; }

	public struct ShootCooldown : IComponentData { public float Value; }

	public struct ShootSpeed : IComponentData { public float Value; }

	public struct Health : IComponentData { public short Value; }

	public struct Targetable : IComponentData { public Entity Value; }

	public struct NeedsTargetTag : IComponentData { public Entity Value; }

	public struct ShipIntent : IComponentData
	{
		public float Turn;    // -1..+1
		public float Thrust;  // -1..+1
		public byte Fire;     // 0/1 (bool in IComponentData is fine but byte is safer/clearer)
	}
}