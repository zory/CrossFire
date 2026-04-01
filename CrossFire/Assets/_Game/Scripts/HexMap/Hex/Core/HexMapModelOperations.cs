using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossFire.HexMap
{
	public static class HexMapModelOperations
	{
		public static bool AddTile(HexMapModel model, Vector3Int tilePosition)
		{
			if (model == null)
			{
				return false;
			}

			if (model.Tiles.ContainsKey(tilePosition))
			{
				return false;
			}

			model.Tiles.Add(tilePosition, model.Tiles.Count);
			return true;
		}

		public static bool RemoveTile(HexMapModel model, Vector3Int tilePosition)
		{
			if (model == null)
			{
				return false;
			}

			bool removedBaseTile = model.Tiles.Remove(tilePosition);
			model.Tiles.Remove(tilePosition);
			return removedBaseTile;
		}

		public static void ReindexTiles(HexMapModel model)
		{
			if (model == null)
			{
				return;
			}

			List<Vector3Int> positions = model.Tiles.Keys.ToList();
			positions.Sort(new Vector3IntComparer());

			model.Tiles.Clear();
			for (int i = 0; i < positions.Count; i++)
			{
				model.Tiles.Add(positions[i], i);
			}
		}

		public static bool SetTeam(HexMapModel model, Vector3Int tilePosition, int teamId)
		{
			if (model == null)
			{
				return false;
			}

			if (!model.Tiles.ContainsKey(tilePosition))
			{
				return false;
			}

			model.TilesToTeamIds[tilePosition] = teamId;
			return true;
		}

		public static bool ClearTeam(HexMapModel model, Vector3Int tilePosition)
		{
			if (model == null)
			{
				return false;
			}

			return model.TilesToTeamIds.Remove(tilePosition);
		}

		public static void Clear(HexMapModel model)
		{
			if (model == null)
			{
				return;
			}

			model.Tiles.Clear();
			model.TilesToTeamIds.Clear();
		}
	}
}
