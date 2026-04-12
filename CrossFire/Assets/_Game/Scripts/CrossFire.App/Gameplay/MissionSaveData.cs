using System;
using Core.Utilities;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// File I/O for the unified <c>.mission</c> file format, which stores both
	/// mission metadata (<see cref="MissionData"/>) and an optional gameplay
	/// simulation snapshot (<see cref="GameplaySimulationSnapshot"/>).
	///
	/// <para>Files are written to
	/// <c>StreamingAssets/Data/Missions/{missionId}.mission</c> as pretty-printed
	/// JSON via <see cref="UnityEngine.JsonUtility"/>.</para>
	///
	/// <para>Metadata and simulation can be saved independently — each method
	/// performs a read-modify-write so one does not overwrite the other.</para>
	/// </summary>
	public static class MissionSaveData
	{
		[Serializable]
		private class MissionFileWrapper
		{
			public MissionData Metadata;
			public bool HasSimulation;
			public GameplaySimulationSnapshot Simulation = new GameplaySimulationSnapshot();
		}

		public const string RELATIVE_PATH = "Data/Missions/";
		public const string EXTENSION = ".mission";

		// ─── Metadata ─────────────────────────────────────────────────────────

		public static void SaveMetadata(int missionId, MissionData metadata)
		{
			MissionFileWrapper wrapper = LoadWrapper(missionId);
			wrapper.Metadata = metadata;
			SaveWrapper(missionId, wrapper);
		}

		public static MissionData LoadMetadata(int missionId)
		{
			return LoadWrapper(missionId).Metadata;
		}

		// ─── Simulation ───────────────────────────────────────────────────────

		public static void SaveSimulation(int missionId, GameplaySimulationSnapshot snapshot)
		{
			MissionFileWrapper wrapper = LoadWrapper(missionId);
			wrapper.HasSimulation = true;
			wrapper.Simulation = snapshot;
			SaveWrapper(missionId, wrapper);
		}

		/// <summary>
		/// Returns null if no simulation has been saved for this mission yet.
		/// </summary>
		public static GameplaySimulationSnapshot LoadSimulation(int missionId)
		{
			MissionFileWrapper wrapper = LoadWrapper(missionId);
			return wrapper.HasSimulation ? wrapper.Simulation : null;
		}

		// ─── Internal helpers ─────────────────────────────────────────────────

		private static MissionFileWrapper LoadWrapper(int missionId)
		{
			string relativePath = RELATIVE_PATH + missionId + EXTENSION;
			string json = PersistentDataHelper.LoadFromFile(relativePath);

			if (string.IsNullOrEmpty(json))
			{
				return new MissionFileWrapper();
			}

			MissionFileWrapper wrapper = JsonUtility.FromJson<MissionFileWrapper>(json);
			return wrapper ?? new MissionFileWrapper();
		}

		private static void SaveWrapper(int missionId, MissionFileWrapper wrapper)
		{
			string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
			string relativePath = RELATIVE_PATH + missionId + EXTENSION;
			PersistentDataHelper.SaveToFile(relativePath, json);
		}
	}
}
