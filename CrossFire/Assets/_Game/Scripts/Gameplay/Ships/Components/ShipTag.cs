using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>Marks an entity as an active ship (not a prefab, not a bullet).</summary>
	public struct ShipTag : IComponentData { }
}