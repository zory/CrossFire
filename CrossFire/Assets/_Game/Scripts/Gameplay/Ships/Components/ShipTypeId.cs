using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Records the <see cref="ShipType"/> used to instantiate this ship entity.
	/// Set by <see cref="ShipsSpawnSystem"/> at spawn time so the type is available
	/// for serialization without consulting the prefab registry.
	/// </summary>
	public struct ShipTypeId : IComponentData
	{
		public ShipType Value;
	}
}
