using Unity.Entities;

namespace Core.Physics
{
	public struct CollisionMask : IComponentData { public uint Value; } // bitset
}