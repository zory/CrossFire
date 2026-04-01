using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
	public class HexMapCreator_Base : MonoBehaviour
	{
		public event Action OnMapCreated;

		public bool EnableBoardCreator = true;
		public GameObject TilePrefab;
		public string FileName = "SavedMap";
		public bool SaveNow;
		public bool LoadNow;

		private Wunderwunsch.HexMapLibrary.HexMap _hexMap;
		private HexMouse _hexMouse;

		private Dictionary<Vector3Int, int> _tilePositionAndIndexDict = new Dictionary<Vector3Int, int>();
		public IReadOnlyDictionary<Vector3Int, int> TilePositionAndIndexDict
		{
			get
			{
				return _tilePositionAndIndexDict;
			}
		}

		private Dictionary<Vector3Int, HexCell> _tilePositionAndGODict = new Dictionary<Vector3Int, HexCell>();
		public IReadOnlyDictionary<Vector3Int, HexCell> TilePositionAndGODict
		{
			get
			{
				return _tilePositionAndGODict;
			}
		}

		public Vector2Int OffsetCoords
		{
			get
			{
				if (_hexMouse == null)
				{
					throw new Exception("_hexMouse is null");
				}
				return _hexMouse.OffsetCoordInfiniteGrid;
			}
		}
		public Vector3Int TileCoords
		{
			get
			{
				return HexConverter.OffsetTileCoordToTileCoord(OffsetCoords);
			}
		}

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
			if (_hexMapHolder != null)
			{
				Destroy(_hexMapHolder);
			}
			_hexMouse.Dispose();
		}

		private void Update()
		{
			if (!EnableBoardCreator)
			{
				return;
			}

			//update the marker positions
			if (Input.GetMouseButtonDown(0))
			{
				if (!_tilePositionAndIndexDict.ContainsKey(TileCoords))
				{
					_tilePositionAndIndexDict.Add(TileCoords, _index);
					_index++;

					DestroyMap();
					CreateMap();
				}
			}
			if (Input.GetMouseButtonDown(1))
			{
				if (_tilePositionAndIndexDict.ContainsKey(TileCoords))
				{
					_tilePositionAndIndexDict.Remove(TileCoords);

					DestroyMap();
					CreateMap();
				}
			}
			if (SaveNow)
			{
				SaveNow = false;
				WorldMapSaveData.SaveWorldMap(FileName, _tilePositionAndIndexDict);
			}
			if (LoadNow)
			{
				LoadNow = false;

				DestroyMap();
				_tilePositionAndIndexDict.Clear();
				_tilePositionAndIndexDict = WorldMapSaveData.LoadWorldMap(FileName);

				CreateMap();
			}
		}

		private void DestroyMap()
		{
			foreach (HexCell hexCell in _tilePositionAndGODict.Values)
			{
				Destroy(hexCell.gameObject);
			}
			_tilePositionAndGODict.Clear();
			_index = 0;
		}

		private void CreateMap()
		{
			var positionCollection = _tilePositionAndIndexDict.Keys.ToList();
			positionCollection.Sort(new Vector3IntComparer());
			_tilePositionAndIndexDict.Clear();
			_index = 0;
			foreach (var position in positionCollection)
			{
				_tilePositionAndIndexDict.Add(position, _index);
				_index++;
			}

			if (_hexMapHolder == null)
			{
				_hexMapHolder = new GameObject("HexGrid");
			}

			//Recreate map and mouse helper with new map
			_hexMap = new Wunderwunsch.HexMapLibrary.HexMap(_tilePositionAndIndexDict, null);
			_hexMouse.UpdateHexMap(_hexMap);

			foreach (var tilePos in _hexMap.TilePositions) //loops through all the tiles, assigns them a random value and instantiates and positions a gameObject for each of them.
			{
				GameObject instance = GameObject.Instantiate(TilePrefab);
				instance.transform.SetParent(_hexMapHolder.transform);
				instance.name = "MapTile_" + tilePos;
				Vector3 position = HexConverter.TileCoordToCartesianCoord(tilePos, yCoord: 0);
				instance.transform.position = position;

				HexCell hexCell = instance.GetComponent<HexCell>();
				_tilePositionAndGODict.Add(tilePos, hexCell);
			}

			OnMapCreated?.Invoke();
		}
	}
}
