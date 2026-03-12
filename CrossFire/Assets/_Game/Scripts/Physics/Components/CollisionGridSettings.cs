using Unity.Entities;

namespace CrossFire.Physics
{
	public struct CollisionGridSettings : IComponentData
	{
		public float CellSize; // e.g. 2..8 world units depending on density/ship size
	}
}