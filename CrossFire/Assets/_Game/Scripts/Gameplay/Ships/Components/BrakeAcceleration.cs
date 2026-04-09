using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Linear acceleration applied when thrust input is negative (braking / reverse).
	/// Units: world-units per second squared at full input.
	/// </summary>
	public struct BrakeAcceleration : IComponentData { public float Value; }
}