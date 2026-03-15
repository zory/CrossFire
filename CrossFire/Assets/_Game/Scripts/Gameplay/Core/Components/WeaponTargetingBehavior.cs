namespace CrossFire.Core
{
	public enum WeaponTargetingBehavior : byte
	{
		DirectFire = 0,     // Shoot at current target position
		LeadFire = 1,       // Predict intercept point
		LockOnTrack = 2,    // Missiles / seekers track entity
		FixedForward = 3    // Fire straight forward, no target needed
	}
}
