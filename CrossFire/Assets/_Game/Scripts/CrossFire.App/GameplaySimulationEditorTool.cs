using Unity.Entities;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Inspector tool for saving and loading the entire gameplay simulation state,
	/// together with the mission metadata (name and description).
	/// Everything is stored in <c>StreamingAssets/Data/Missions/{MissionId}.mission</c>
	/// via <see cref="MissionSaveData"/>.
	///
	/// <para>Tick <see cref="_saveNow"/> to capture the current ECS world and metadata to file.</para>
	/// <para>Tick <see cref="_loadNow"/> to destroy the current simulation, restore it from file,
	/// and auto-populate the name/description fields from the saved metadata.</para>
	///
	/// Intended to be used together with <see cref="SimulationEditingTool"/>: pause the simulation
	/// first, then save/load freely without the world advancing between ticks.
	/// </summary>
	public class GameplaySimulationEditorTool : MonoBehaviour
	{
		[SerializeField]
		private int _missionId;

		[SerializeField]
		private string _missionName;

		[SerializeField]
		[TextArea(2, 5)]
		private string _missionDescription;

		[SerializeField]
		private bool _saveNow;

		[SerializeField]
		private bool _loadNow;

		private void Update()
		{
			if (_saveNow)
			{
				_saveNow = false;
				Save();
			}

			if (_loadNow)
			{
				_loadNow = false;
				Load();
			}
		}

		private void Save()
		{
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
			{
				Debug.LogWarning("[GameplaySimulationEditorTool] No active ECS world — save aborted.");
				return;
			}

			GameplaySimulationSnapshot snapshot = GameplaySimulationSerializer.CaptureSnapshot(world.EntityManager);
			MissionSaveData.SaveSimulation(_missionId, snapshot);

			MissionData metadata = new MissionData
			{
				Id          = _missionId,
				Name        = _missionName,
				Description = _missionDescription,
			};
			MissionSaveData.SaveMetadata(_missionId, metadata);

			Debug.Log($"[GameplaySimulationEditorTool] Saved mission {_missionId} \"{_missionName}\": " +
			          $"{snapshot.Ships?.Length ?? 0} ships, {snapshot.Bullets?.Length ?? 0} bullets.");
		}

		private void Load()
		{
			GameplaySimulationSnapshot snapshot = MissionSaveData.LoadSimulation(_missionId);
			if (snapshot == null)
			{
				Debug.LogWarning($"[GameplaySimulationEditorTool] No simulation save found for mission {_missionId}.");
				return;
			}

			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
			{
				Debug.LogWarning("[GameplaySimulationEditorTool] No active ECS world — load aborted.");
				return;
			}

			GameplaySimulationSerializer.RestoreSnapshot(snapshot, world.EntityManager);

			MissionData metadata = MissionSaveData.LoadMetadata(_missionId);
			_missionName        = metadata.Name;
			_missionDescription = metadata.Description;

			Debug.Log($"[GameplaySimulationEditorTool] Loaded mission {_missionId} \"{_missionName}\": " +
			          $"{snapshot.Ships?.Length ?? 0} ships, {snapshot.Bullets?.Length ?? 0} bullets.");
		}
	}
}
