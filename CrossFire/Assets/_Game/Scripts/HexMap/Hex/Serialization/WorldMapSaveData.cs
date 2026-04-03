using CrossFire.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public static class WorldMapSaveData
	{
		[System.Serializable]
		public class WorldMapSaveDataWrapper
		{
			public List<Vector2Int> TilePositions = new List<Vector2Int>();
		}

		public const string RELATIVE_WORLD_MAPS_PATH = "Data/WorldMaps/";
		public const string WORLD_MAPS_EXTENSION = ".wm";

		public static void SaveWorldMap(string fileName, Dictionary<Vector3Int, int> tileIndexByPosition)
		{
			WorldMapSaveDataWrapper wrapper = new WorldMapSaveDataWrapper();

			foreach (KeyValuePair<Vector3Int, int> tileIndexAndPosition in tileIndexByPosition)
			{
				Vector2Int offsetTilePosition = HexConverter.TileCoordToOffsetTileCoord(tileIndexAndPosition.Key);
				wrapper.TilePositions.Add(offsetTilePosition);
			}

			SaveDataFileHelper.SaveWrapper(
				RELATIVE_WORLD_MAPS_PATH,
				WORLD_MAPS_EXTENSION,
				fileName,
				wrapper
			);
		}

		public static Dictionary<Vector3Int, int> LoadWorldMap(string fileName)
		{
			WorldMapSaveDataWrapper wrapper = SaveDataFileHelper.LoadWrapper(
				RELATIVE_WORLD_MAPS_PATH,
				WORLD_MAPS_EXTENSION,
				fileName,
				() => new WorldMapSaveDataWrapper()
			);

			Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();
			int index = 0;

			foreach (Vector2Int offsetTilePosition in wrapper.TilePositions)
			{
				Vector3Int tilePosition = HexConverter.OffsetTileCoordToTileCoord(offsetTilePosition);

				if (!result.ContainsKey(tilePosition))
				{
					result.Add(tilePosition, index);
					index++;
				}
			}

			return result;
		}
	}

	public static class WorldMapOwnersSaveData
	{
		[System.Serializable]
		public struct TileOwnerEntry
		{
			public Vector2Int TilePosition;
			public int TeamId;
		}

		[System.Serializable]
		public class WorldMapOwnersSaveDataWrapper
		{
			public List<TileOwnerEntry> Entries = new List<TileOwnerEntry>();
		}

		public const string RELATIVE_WORLD_CELL_OWNERS_PATH = "Data/WorldMapCellOwners/";
		public const string WORLD_CELL_OWNERS_EXTENSION = ".wmo";

		public static void SaveWorldMapOwners(string fileName, Dictionary<Vector3Int, int> tilePositionsToTeamId)
		{
			WorldMapOwnersSaveDataWrapper wrapper = new WorldMapOwnersSaveDataWrapper();

			foreach (KeyValuePair<Vector3Int, int> tileIndexAndPosition in tilePositionsToTeamId)
			{
				TileOwnerEntry entry = new TileOwnerEntry
				{
					TilePosition = HexConverter.TileCoordToOffsetTileCoord(tileIndexAndPosition.Key),
					TeamId = tileIndexAndPosition.Value
				};

				wrapper.Entries.Add(entry);
			}

			SaveDataFileHelper.SaveWrapper(
				RELATIVE_WORLD_CELL_OWNERS_PATH,
				WORLD_CELL_OWNERS_EXTENSION,
				fileName,
				wrapper
			);
		}

		public static Dictionary<Vector3Int, int> LoadWorldMapOwners(string fileName)
		{
			WorldMapOwnersSaveDataWrapper wrapper = SaveDataFileHelper.LoadWrapper(
				RELATIVE_WORLD_CELL_OWNERS_PATH,
				WORLD_CELL_OWNERS_EXTENSION,
				fileName,
				() => new WorldMapOwnersSaveDataWrapper()
			);

			Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();

			foreach (TileOwnerEntry entry in wrapper.Entries)
			{
				Vector3Int tilePosition = HexConverter.OffsetTileCoordToTileCoord(entry.TilePosition);

				if (!result.ContainsKey(tilePosition))
				{
					result.Add(tilePosition, entry.TeamId);
				}
			}

			return result;
		}
	}

	public static class WorldMapMissionsSaveData
	{
		[System.Serializable]
		public struct TileMissionEntry
		{
			public Vector2Int TilePosition;
			public int MissionId;
		}

		[System.Serializable]
		public class WorldMapMissionsSaveDataWrapper
		{
			public List<TileMissionEntry> Entries = new List<TileMissionEntry>();
		}

		public const string RELATIVE_WORLD_MISSIONS_PATH = "Data/WorldMapMissions/";
		public const string WORLD_MISSIONS_EXTENSION = ".wmm";

		public static void SaveWorldMapMissions(string fileName, Dictionary<Vector3Int, int> tilePositionsToMissionId)
		{
			WorldMapMissionsSaveDataWrapper wrapper = new WorldMapMissionsSaveDataWrapper();

			foreach (KeyValuePair<Vector3Int, int> pair in tilePositionsToMissionId)
			{
				TileMissionEntry entry = new TileMissionEntry
				{
					TilePosition = HexConverter.TileCoordToOffsetTileCoord(pair.Key),
					MissionId = pair.Value
				};

				wrapper.Entries.Add(entry);
			}

			SaveDataFileHelper.SaveWrapper(
				RELATIVE_WORLD_MISSIONS_PATH,
				WORLD_MISSIONS_EXTENSION,
				fileName,
				wrapper
			);
		}

		public static Dictionary<Vector3Int, int> LoadWorldMapMissions(string fileName)
		{
			WorldMapMissionsSaveDataWrapper wrapper = SaveDataFileHelper.LoadWrapper(
				RELATIVE_WORLD_MISSIONS_PATH,
				WORLD_MISSIONS_EXTENSION,
				fileName,
				() => new WorldMapMissionsSaveDataWrapper()
			);

			Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();

			foreach (TileMissionEntry entry in wrapper.Entries)
			{
				Vector3Int tilePosition = HexConverter.OffsetTileCoordToTileCoord(entry.TilePosition);

				if (!result.ContainsKey(tilePosition))
				{
					result.Add(tilePosition, entry.MissionId);
				}
			}

			return result;
		}
	}

	public static class SaveDataFileHelper
	{
		public static void SaveWrapper<TWrapper>(string relativeFolderPath, string fileExtension, string fileName, TWrapper wrapper)
		{
			string json = JsonUtility.ToJson(wrapper, true);
			string relativePath = relativeFolderPath + fileName + fileExtension;
			PersistentDataHelper.SaveToFile(relativePath, json);
		}

		public static TWrapper LoadWrapper<TWrapper>(string relativeFolderPath, string fileExtension, string fileName, Func<TWrapper> createDefaultWrapper)
		{
			string relativePath = relativeFolderPath + fileName + fileExtension;
			string json = PersistentDataHelper.LoadFromFile(relativePath);

			if (string.IsNullOrEmpty(json))
			{
				return createDefaultWrapper();
			}

			TWrapper wrapper = JsonUtility.FromJson<TWrapper>(json);
			if (wrapper == null)
			{
				return createDefaultWrapper();
			}

			return wrapper;
		}
	}
}
