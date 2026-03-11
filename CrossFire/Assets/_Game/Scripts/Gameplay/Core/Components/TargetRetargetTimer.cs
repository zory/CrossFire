using Unity.Entities;

namespace CrossFire.Combat
{
	public struct TargetRetargetTimer : IComponentData
	{
		public float TimeLeft;
	}
}