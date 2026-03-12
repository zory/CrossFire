using Unity.Entities;

namespace CrossFire.Physics
{
	public struct Collider2D : IComponentData
	{
		public Collider2DType Type;
		public float BoundRadius; // broadphase bound radius in WORLD units
		public float CircleRadius; // only used when Type == Circle
	}
}