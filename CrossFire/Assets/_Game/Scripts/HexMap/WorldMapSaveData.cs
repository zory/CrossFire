using CrossFire.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
    public static class WorldMapSaveData
    {
		public class WorlMapSaveDataWrapper
		{
			public List<Vector2Int> TilePositions;
		}

		public const string RELATIVE_WORLD_MAPS_PATH = "Data/WorldMaps/";
		public const string WORLD_MAPS_EXTENSION = ".wm";

		public static void SaveWorldMap(string fileName, Dictionary<Vector3Int, int> tileIndexByPosition)
		{
			List<Vector2Int> positions = new List<Vector2Int>();
			foreach (var tileIndexAndPosition in tileIndexByPosition)
			{
				positions.Add(HexConverter.TileCoordToOffsetTileCoord(tileIndexAndPosition.Key));
			}
			WorlMapSaveDataWrapper wrapper = new WorlMapSaveDataWrapper
			{
				TilePositions = positions,
			};
			SaveWorldMapWrapper(fileName, wrapper);
		}

		public static Dictionary<Vector3Int, int> LoadWorldMap(string fileName)
		{
			Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();
			WorlMapSaveDataWrapper wrapper = LoadWorldMapWrapper(fileName);
			int index = 0;
			foreach (var offsetPos in wrapper.TilePositions)
			{
				Vector3Int tilePos = HexConverter.OffsetTileCoordToTileCoord(offsetPos);
				if (!result.ContainsKey(tilePos))
				{
					result.Add(tilePos, index);
					index++;
				}
			}
			return result;
		}

		public static void SaveWorldMapWrapper(string fileName, WorlMapSaveDataWrapper worldMapWrapper)
		{
			string json = JsonUtility.ToJson(worldMapWrapper, true);
			string relativePath = RELATIVE_WORLD_MAPS_PATH + fileName + WORLD_MAPS_EXTENSION;
			PersistentDataHelper.SaveToFile(relativePath, json);
		}

		public static WorlMapSaveDataWrapper LoadWorldMapWrapper(string fileName)
		{
			string relativePath = RELATIVE_WORLD_MAPS_PATH + fileName + WORLD_MAPS_EXTENSION;
			string json = PersistentDataHelper.LoadFromFile(relativePath);
			if (string.IsNullOrEmpty(json))
			{
				return new WorlMapSaveDataWrapper() { TilePositions = new List<Vector2Int>() };
			}

			WorlMapSaveDataWrapper worldMapWrapper = JsonUtility.FromJson<WorlMapSaveDataWrapper>(json);
			return worldMapWrapper;
		}
	}
}
