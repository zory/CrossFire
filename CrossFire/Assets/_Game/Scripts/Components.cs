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
}