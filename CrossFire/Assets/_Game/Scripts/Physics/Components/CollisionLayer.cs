using Unity.Entities;

namespace Core.Physics
{
	public struct CollisionLayer : IComponentData { public uint Value; } // one-hot
}