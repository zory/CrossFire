using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public class Vector3IntComparer : IComparer<Vector3Int>
	{
		public int Compare(Vector3Int a, Vector3Int b)
		{
			// First: sort by z (row-like)
			if (a.z != b.z)
			{
				return a.z.CompareTo(b.z);
			}

			// Then: sort by x (column-like)
			if (a.x != b.x)
			{
				return a.x.CompareTo(b.x);
			}

			// Optional: y fallback (should be redundant if cube coords are valid)
			return a.y.CompareTo(b.y);
		}
	}

	public class MapDataWrapper
	{
		public List<Vector2Int> TilePositions;
	}

	public class BoardCreater : MonoBehaviour
	{
		public GameObject TilePrefab;
		public string FileName = "SavedMap";
		public bool SaveNow;

		private Wunderwunsch.HexMapLibrary.HexMap _hexMap;
		private HexMouse _hexMouse;

		private Dictionary<Vector3Int, int> _tileIndexByPosition = new Dictionary<Vector3Int, int>();
		private int _index = 0;

		private GameObject _hexMapHolder;

		private void Start()
		{
			_hexMouse = new HexMouse();
			_hexMouse.Init(useMonoBehaviourHelper: true);
			CreateMap();
		}

		private void OnDestroy()
		{
			_hexMouse.Dispose();
		}

		private void Update()
		{
			Vector2Int offsetCoords = _hexMouse.OffsetCoordInfiniteGrid;
			Vector3Int tileCoords = HexConverter.OffsetTileCoordToTileCoord(offsetCoords);
			//update the marker positions
			if (Input.GetMouseButtonDown(0))
			{
				if (!_tileIndexByPosition.ContainsKey(tileCoords))
				{
					_tileIndexByPosition.Add(tileCoords, _index);
					_index++;

					DestroyMap();
					CreateMap();
				}
			}
			if (Input.GetMouseButtonDown(1))
			{
				if (_tileIndexByPosition.ContainsKey(tileCoords))
				{
					_tileIndexByPosition.Remove(tileCoords);

					DestroyMap();
					CreateMap();
				}
			}
			if (SaveNow)
			{
				SaveNow = false;
				Save();
			}
		}

		private void DestroyMap()
		{
			Destroy(_hexMapHolder);
			_index = 0;
		}

		private void CreateMap()
		{
			var positionCollection = _tileIndexByPosition.Keys.ToList();
			positionCollection.Sort(new Vector3IntComparer());
			_tileIndexByPosition.Clear();
			foreach (var position in positionCollection)
			{
				_tileIndexByPosition.Add(position, _index);
				_index++;
			}

			_hexMapHolder = new GameObject("HexGrid");
			_hexMap = new Wunderwunsch.HexMapLibrary.HexMap(_tileIndexByPosition, null);
			_hexMouse.UpdateHexMap(_hexMap);

			foreach (var tilePos in _hexMap.TilePositions) //loops through all the tiles, assigns them a random value and instantiates and positions a gameObject for each of them.
			{
				GameObject instance = GameObject.Instantiate(TilePrefab);
				instance.transform.SetParent(_hexMapHolder.transform);
				instance.name = "MapTile_" + tilePos;
				Vector3 position = HexConverter.TileCoordToCartesianCoord(tilePos, yCoord: 0);
				instance.transform.position = position;
			}
		}

		private void Save()
		{
			List<Vector2Int> positions = new List<Vector2Int>();
			foreach (var tilePos in _tileIndexByPosition)
			{
				positions.Add(HexConverter.TileCoordToOffsetTileCoord(tilePos.Key));
			}
			MapDataWrapper wrapper = new MapDataWrapper
			{
				TilePositions = positions,
			};

			string json = JsonUtility.ToJson(wrapper, true);
			UnityEngine.Debug.Log("SaveData: " + json);
			SaveToFile(json);
		}

		private void SaveToFile(string content)
		{
			// Ensure folder exists
			Directory.CreateDirectory(Application.streamingAssetsPath);

			string path = Path.Combine(Application.streamingAssetsPath, "Data");
			Directory.CreateDirectory(path);

			path = Path.Combine(path, "WorldMaps");
			Directory.CreateDirectory(path);

			path = Path.Combine(path, FileName + ".wm");

			File.WriteAllText(path, content);
		}
	}
}
