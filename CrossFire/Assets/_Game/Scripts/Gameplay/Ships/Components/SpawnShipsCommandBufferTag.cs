using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Singleton tag that marks the entity holding the
	/// <see cref="DynamicBuffer{T}"/> of <see cref="SpawnShipsCommand"/> requests.
	/// Created once by <see cref="ShipsSpawnCommandBufferSystem"/> and never removed.
	/// </summary>
	public struct SpawnShipsCommandBufferTag : IComponentData { }
}