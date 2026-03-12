using Unity.Entities;

namespace CrossFire.Physics
{
	public struct CollisionLayer : IComponentData { public uint Value; } // one-hot
}