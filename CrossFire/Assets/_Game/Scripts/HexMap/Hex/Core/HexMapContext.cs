using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public sealed class HexMapContext
	{
		public HexMapModel Model { get; } = new();
		public Wunderwunsch.HexMapLibrary.HexMap HexMap { get; private set; }

		public void RebuildHexMap()
		{
			HexMap = new Wunderwunsch.HexMapLibrary.HexMap(Model.Tiles, null);
		}
	}
}
