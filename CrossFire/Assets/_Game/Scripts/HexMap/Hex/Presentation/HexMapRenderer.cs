using System.Collections.Generic;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public class HexMapRenderer : MonoBehaviour
	{
		[SerializeField]
		private GameObject tilePrefab;

		private readonly Dictionary<Vector3Int, HexTile> _cellsByPosition = new Dictionary<Vector3Int, HexTile>();
		private GameObject _holder;

		public IReadOnlyDictionary<Vector3Int, HexTile> CellsByPosition => _cellsByPosition;

		public void Render(HexMapContext context)
		{
			Clear();

			if (_holder == null)
			{
				_holder = new GameObject("HexGrid");
				_holder.transform.SetParent(transform, false);
			}

			foreach (Vector3Int tilePosition in context.Model.Tiles.Keys)
			{
				GameObject instance = Instantiate(tilePrefab, _holder.transform);
				instance.name = "MapTile_" + tilePosition;
				instance.transform.position = HexConverter.TileCoordToCartesianCoord(tilePosition, 0);

				HexTile hexTile = instance.GetComponent<HexTile>();
				_cellsByPosition.Add(tilePosition, hexTile);
			}
		}

		public void Clear()
		{
			foreach (HexTile cell in _cellsByPosition.Values)
			{
				if (cell != null)
				{
					Destroy(cell.gameObject);
				}
			}

			_cellsByPosition.Clear();
		}
	}
}
