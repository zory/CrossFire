using Unity.Entities;

namespace CrossFire.Core
{
	public struct WeaponTarget : IBufferElementData
	{
		public byte WeaponSlotIndex;
		public WeaponTargetingBehavior Behavior;
		public TargetReference Target;
	}
}
