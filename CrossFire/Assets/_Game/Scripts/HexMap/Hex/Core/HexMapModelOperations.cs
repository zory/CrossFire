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

			model.TilesToTeamIds.Remove(tilePosition);
			model.TilesToMissionIds.Remove(tilePosition);
			return model.Tiles.Remove(tilePosition);
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

		public static bool SetMission(HexMapModel model, Vector3Int tilePosition, int missionId)
		{
			if (model == null)
			{
				return false;
			}

			if (!model.Tiles.ContainsKey(tilePosition))
			{
				return false;
			}

			model.TilesToMissionIds[tilePosition] = missionId;
			return true;
		}

		public static bool ClearMission(HexMapModel model, Vector3Int tilePosition)
		{
			if (model == null)
			{
				return false;
			}

			return model.TilesToMissionIds.Remove(tilePosition);
		}

		public static void Clear(HexMapModel model)
		{
			if (model == null)
			{
				return;
			}

			model.Tiles.Clear();
			model.TilesToTeamIds.Clear();
			model.TilesToMissionIds.Clear();
		}
	}
}
