using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// Linear acceleration applied when thrust input is positive (forward thrust).
	/// Units: world-units per second squared at full input.
	/// </summary>
	public struct ThrustAcceleration : IComponentData { public float Value; }
}