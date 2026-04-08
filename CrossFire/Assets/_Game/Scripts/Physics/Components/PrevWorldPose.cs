using Unity.Entities;

namespace Core.Physics
{
	public struct PrevWorldPose : IComponentData { public Pose2D Value; }
}