using Unity.Entities;

namespace Core.Physics
{
	public struct AngularVelocity : IComponentData
	{
		// radians per second
		public float Value;
	}
}