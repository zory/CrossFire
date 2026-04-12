using CrossFire.Core;
using Unity.Entities;

namespace CrossFire.App
{
	/// <summary>
	/// Public API for pausing and resuming the gameplay simulation.
	/// Pause state is represented by a <see cref="SimulationPaused"/> singleton component.
	/// <see cref="AppSimulationPipeline"/> checks for it at the start of each frame and skips
	/// its update entirely when the component is present. Non-gameplay systems (UI, particles,
	/// audio) are unaffected because they run outside the pipeline.
	/// </summary>
	public static class SimulationPauseApi
	{
		/// <summary>
		/// Pauses the simulation by creating the <see cref="SimulationPaused"/> singleton.
		/// Idempotent — safe to call if already paused.
		/// </summary>
		public static void Pause(EntityManager entityManager)
		{
			if (IsPaused(entityManager))
			{
				return;
			}
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new SimulationPaused());
		}

		/// <summary>
		/// Resumes the simulation by destroying the <see cref="SimulationPaused"/> singleton.
		/// Idempotent — safe to call if not currently paused.
		/// </summary>
		public static void Resume(EntityManager entityManager)
		{
			using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SimulationPaused>());
			if (!query.IsEmpty)
			{
				entityManager.DestroyEntity(query.GetSingletonEntity());
			}
		}

		/// <summary>
		/// Returns true when the simulation is currently paused.
		/// </summary>
		public static bool IsPaused(EntityManager entityManager)
		{
			using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SimulationPaused>());
			return !query.IsEmpty;
		}
	}
}
