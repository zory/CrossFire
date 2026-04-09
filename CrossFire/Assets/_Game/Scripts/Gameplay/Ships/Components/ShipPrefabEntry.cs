using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Buffer element that maps a <see cref="ShipType"/> to its baked prefab entity.
	/// Lives on the registry singleton created by <see cref="ShipPrefabRegistryAuthoring"/>.
	/// Read by <see cref="ShipsSpawnSystem"/> to resolve which prefab to instantiate.
	/// </summary>
	public struct ShipPrefabEntry : IBufferElementData
	{
		public ShipType Type;
		public Entity Prefab;
	}
}
