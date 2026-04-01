using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class BaseTilesLayerSerializer : IHexMapLayerSerializer
	{
		public void Save(string fileName, HexMapModel model)
		{
			Dictionary<Vector3Int, int> copy = new Dictionary<Vector3Int, int>(model.Tiles);
			WorldMapSaveData.SaveWorldMap(fileName, copy);
		}

		public void LoadInto(string fileName, HexMapModel model)
		{
			model.Tiles.Clear();
			Dictionary<Vector3Int, int> loaded = WorldMapSaveData.LoadWorldMap(fileName);
			foreach (KeyValuePair<Vector3Int, int> pair in loaded)
			{
				model.Tiles[pair.Key] = pair.Value;
			}
		}
	}
}
