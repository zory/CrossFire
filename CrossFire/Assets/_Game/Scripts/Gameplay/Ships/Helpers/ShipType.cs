namespace CrossFire.Ships
{
	/// <summary>
	/// Identifies which ship prefab to instantiate.
	/// Used as a key into the <see cref="ShipPrefabEntry"/> registry.
	/// </summary>
	public enum ShipType : int
	{
		Fighter = 0,
		Bomber  = 1,
		Carrier = 2,

		// Placeholder types used by physics/collision samples.
		Sample1 = 3,
		Sample2 = 4,
		Sample3 = 5,
	}
}