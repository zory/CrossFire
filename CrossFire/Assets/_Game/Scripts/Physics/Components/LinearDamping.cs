using Unity.Entities;

namespace CrossFire.Physics
{
	public struct LinearDamping : IComponentData
	{
		public float Value; // per second
	}
}