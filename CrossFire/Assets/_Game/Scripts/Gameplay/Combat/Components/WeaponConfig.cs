using Unity.Entities;

namespace CrossFire.Combat
{
	public struct WeaponConfig : IComponentData
	{
		public float FireInterval;     // seconds between shots
		public float BulletSpeed;
		public float BulletLifetime;
		public float MuzzleOffset;     // forward offset from ship position
	}
}