using Unity.Entities;

namespace Core.Physics
{
	public struct Collider2D : IComponentData
	{
		public Collider2DType Type;
		public float BoundRadius; // broadphase bound radius in WORLD units
		public float CircleRadius; // only used when Type == Circle
	}
}