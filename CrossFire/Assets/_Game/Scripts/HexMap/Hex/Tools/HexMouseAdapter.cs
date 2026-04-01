using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public class HexMouseAdapter : MonoBehaviour
	{
		private HexMouse _hexMouse;

		public bool CursorOnMap => _hexMouse != null && _hexMouse.CursorIsOnMap;
		public Vector2Int OffsetCoords => _hexMouse != null ? _hexMouse.OffsetCoordInfiniteGrid : default;
		public Vector3Int TileCoords => HexConverter.OffsetTileCoordToTileCoord(OffsetCoords);
		public Vector3 CartesianCoords => _hexMouse != null ? _hexMouse.CartesianCoordInfiniteGrid : default;

		public void Bind(Wunderwunsch.HexMapLibrary.HexMap hexMap)
		{
			if (_hexMouse == null)
			{
				_hexMouse = new HexMouse();
				_hexMouse.Init(useMonoBehaviourHelper: true);
			}

			_hexMouse.UpdateHexMap(hexMap);
		}

		private void OnDestroy()
		{
			if (_hexMouse != null)
			{
				_hexMouse.Dispose();
			}
		}
	}
}
