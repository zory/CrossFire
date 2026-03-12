using Unity.Entities;

namespace CrossFire.Physics
{
	public struct CollisionEvent : IBufferElementData
	{
		public Entity FirstEntity;
		public Entity SecondEntity;
	}
}