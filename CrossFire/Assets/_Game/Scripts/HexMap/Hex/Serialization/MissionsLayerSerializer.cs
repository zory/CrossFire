using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class MissionsLayerSerializer : IHexMapLayerSerializer
	{
		public void Save(string fileName, HexMapModel model)
		{
			Dictionary<Vector3Int, int> copy = new Dictionary<Vector3Int, int>(model.TilesToMissionIds);
			WorldMapMissionsSaveData.SaveWorldMapMissions(fileName, copy);
		}

		public void LoadInto(string fileName, HexMapModel model)
		{
			model.TilesToMissionIds.Clear();
			Dictionary<Vector3Int, int> loaded = WorldMapMissionsSaveData.LoadWorldMapMissions(fileName);
			foreach (KeyValuePair<Vector3Int, int> pair in loaded)
			{
				if (model.Tiles.ContainsKey(pair.Key))
				{
					model.TilesToMissionIds[pair.Key] = pair.Value;
				}
			}
		}
	}
}
