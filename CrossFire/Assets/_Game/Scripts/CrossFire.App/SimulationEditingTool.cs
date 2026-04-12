using Unity.Entities;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Inspector toggle that pauses and resumes the gameplay simulation.
	/// Tick <see cref="_enableEditing"/> to freeze the simulation so gameplay entities can be
	/// inspected or modified without the world advancing. Untick to resume.
	///
	/// Place on any GameObject in the Gameplay scene.
	/// When this component is disabled or destroyed while editing, the simulation is
	/// automatically resumed so the game does not stay stuck in a paused state.
	/// </summary>
	public class SimulationEditingTool : MonoBehaviour
	{
		[SerializeField]
		private bool _enableEditing;

		private bool _previousEnableEditing;

		private void Update()
		{
			if (_enableEditing == _previousEnableEditing)
			{
				return;
			}

			_previousEnableEditing = _enableEditing;
			ApplyEditingState(_enableEditing);
		}

		private void OnDisable()
		{
			// If editing was active when the component is disabled or the GameObject is
			// deactivated, resume the simulation so it does not stay frozen.
			if (_previousEnableEditing)
			{
				_previousEnableEditing = false;
				ApplyEditingState(false);
			}
		}

		private static void ApplyEditingState(bool isEditing)
		{
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
			{
				Debug.LogWarning("[SimulationEditingTool] No active ECS world found.");
				return;
			}

			EntityManager entityManager = world.EntityManager;
			if (isEditing)
			{
				SimulationPauseApi.Pause(entityManager);
			}
			else
			{
				SimulationPauseApi.Resume(entityManager);
			}
		}
	}
}
