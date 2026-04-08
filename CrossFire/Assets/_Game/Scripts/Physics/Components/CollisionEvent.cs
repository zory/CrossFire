using Unity.Entities;

namespace Core.Physics
{
	public struct CollisionEvent : IBufferElementData
	{
		public Entity FirstEntity;
		public Entity SecondEntity;
	}
}