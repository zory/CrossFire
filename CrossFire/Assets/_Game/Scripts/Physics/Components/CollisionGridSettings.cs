using Unity.Entities;

namespace Core.Physics
{
	public struct CollisionGridSettings : IComponentData
	{
		public float CellSize; // e.g. 2..8 world units depending on density/ship size
	}
}