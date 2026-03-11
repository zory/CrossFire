using Unity.Entities;

namespace CrossFire.Combat
{
	public struct WeaponCooldown : IComponentData
	{
		public float TimeLeft; // seconds
	}
}