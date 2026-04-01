using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class HexMapModel
	{
		public Dictionary<Vector3Int, int> Tiles { get; } = new Dictionary<Vector3Int, int>();
		public Dictionary<Vector3Int, int> TilesToTeamIds { get; } = new Dictionary<Vector3Int, int>();
	}
}
