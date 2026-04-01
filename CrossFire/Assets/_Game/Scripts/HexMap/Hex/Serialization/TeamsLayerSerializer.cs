using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class TeamsLayerSerializer : IHexMapLayerSerializer
	{
		public void Save(string fileName, HexMapModel model)
		{
			Dictionary<Vector3Int, int> filtered = new Dictionary<Vector3Int, int>();
			foreach (KeyValuePair<Vector3Int, int> pair in model.Tiles)
			{
				if (model.Tiles.ContainsKey(pair.Key))
				{
					filtered[pair.Key] = pair.Value;
				}
			}

			WorldMapOwnersSaveData.SaveWorldMapOwners(fileName, filtered);
		}

		public void LoadInto(string fileName, HexMapModel model)
		{
			model.Tiles.Clear();
			Dictionary<Vector3Int, int> loaded = WorldMapOwnersSaveData.LoadWorldMapOwners(fileName);
			foreach (KeyValuePair<Vector3Int, int> pair in loaded)
			{
				if (model.Tiles.ContainsKey(pair.Key))
				{
					model.TilesToTeamIds[pair.Key] = pair.Value;
				}
			}
		}
	}
}