using Unity.Entities;

namespace CrossFire.Combat
{
	public struct TargetingProfile : IComponentData
	{
		public TargetingMode Mode;
		public float RetargetInterval;
	}
}