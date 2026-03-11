using Unity.Entities;

namespace CrossFire.Ships
{
	public struct TurnSpeed : IComponentData
	{
		public float Value; // radians per second at full input
	}
}