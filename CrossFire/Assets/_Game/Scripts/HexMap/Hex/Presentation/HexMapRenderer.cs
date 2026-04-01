using System.Collections.Generic;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public class HexMapRenderer : MonoBehaviour
	{
		[SerializeField]
		private GameObject tilePrefab;

		private readonly Dictionary<Vector3Int, HexCell> _cellsByPosition = new Dictionary<Vector3Int, HexCell>();
		private GameObject _holder;

		public IReadOnlyDictionary<Vector3Int, HexCell> CellsByPosition => _cellsByPosition;

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

				HexCell hexCell = instance.GetComponent<HexCell>();
				_cellsByPosition.Add(tilePosition, hexCell);
			}
		}

		public void Clear()
		{
			foreach (HexCell cell in _cellsByPosition.Values)
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
