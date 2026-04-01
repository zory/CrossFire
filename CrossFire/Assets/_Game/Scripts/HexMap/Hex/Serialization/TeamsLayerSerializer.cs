using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class TeamsLayerSerializer : IHexMapLayerSerializer
	{
		public void Save(string fileName, HexMapModel model)
		{
			Dictionary<Vector3Int, int> copy = new Dictionary<Vector3Int, int>(model.TilesToTeamIds);
			WorldMapOwnersSaveData.SaveWorldMapOwners(fileName, copy);
		}

		public void LoadInto(string fileName, HexMapModel model)
		{
			model.TilesToTeamIds.Clear();
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