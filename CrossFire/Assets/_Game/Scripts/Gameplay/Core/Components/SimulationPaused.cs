using Unity.Entities;

namespace CrossFire.Core
{
	/// <summary>
	/// Singleton tag. When an entity with this component exists,
	/// <see cref="CrossFire.App.AppSimulationPipeline"/> skips its update entirely,
	/// freezing all gameplay simulation. Non-gameplay systems (UI, particles, audio)
	/// are unaffected. Use <see cref="CrossFire.App.SimulationPauseApi"/> to add and remove it.
	/// </summary>
	public struct SimulationPaused : IComponentData {}
}
