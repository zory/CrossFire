using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class HexMapModel
	{
		// Maps tile position (cube coords) to sorted index — used for array-based access.
		public Dictionary<Vector3Int, int> Tiles { get; } = new Dictionary<Vector3Int, int>();

		// Maps tile position (cube coords) to a team ID — used by game logic and visual layers.
		public Dictionary<Vector3Int, int> TilesToTeamIds { get; } = new Dictionary<Vector3Int, int>();

		// Maps tile position (cube coords) to a mission ID — used by game logic and visual layers.
		public Dictionary<Vector3Int, int> TilesToMissionIds { get; } = new Dictionary<Vector3Int, int>();
	}
}
