using Unity.Entities;

namespace CrossFire.Core
{
	public struct TargetingProfile : IComponentData
	{
		public TargetingMode Mode;
		public float RetargetInterval;
	}
}