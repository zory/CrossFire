using Unity.Entities;

namespace Core.Physics
{
	public struct LinearDamping : IComponentData
	{
		public float Value; // per second
	}
}