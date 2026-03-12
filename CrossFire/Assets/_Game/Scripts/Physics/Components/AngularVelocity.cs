using Unity.Entities;

namespace CrossFire.Physics
{
	public struct AngularVelocity : IComponentData
	{
		// radians per second
		public float Value;
	}
}