using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Maximum angular speed reached when turn input is at full deflection (±1).
	/// Units: radians per second.
	/// </summary>
	public struct TurnSpeed : IComponentData
	{
		public float Value;
	}
}