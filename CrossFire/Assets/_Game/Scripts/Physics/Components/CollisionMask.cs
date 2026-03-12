using Unity.Entities;

namespace CrossFire.Physics
{
	public struct CollisionMask : IComponentData { public uint Value; } // bitset
}