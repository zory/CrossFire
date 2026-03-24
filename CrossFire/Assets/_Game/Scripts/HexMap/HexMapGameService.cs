using System.Collections.Generic;
using UnityEngine;
using Wunderwunsch.HexMapLibrary;
using Wunderwunsch.HexMapLibrary.Generic;

namespace CrossFire.HexMap
{
    public class HexMapGameService : MonoBehaviour
    {
		[SerializeField]
		private Vector2Int mapSize = new Vector2Int(13, 13);
		[SerializeField]
		private GameObject tilePrefab = null;

		private HexMap<int, bool> hexMap;
		private HexMouse hexMouse = null;
		private GameObject[] tileObjects;

		private GameObject selectedTileObject;

		private void Start()
		{
			hexMap = new HexMap<int, bool>(HexMapBuilder.CreateRectangularShapedMap(mapSize), null);
			hexMouse = new HexMouse();
			hexMouse.Init(hexMap, useMonoBehaviourHelper: true);
			tileObjects = new GameObject[hexMap.TilesByPosition.Count];
			var parentGO = new GameObject("HexGrid");
			foreach (var tile in hexMap.Tiles) //loops through all the tiles, assigns them a random value and instantiates and positions a gameObject for each of them.
			{
				tile.Data = (Random.Range(0, 4));
				GameObject instance = GameObject.Instantiate(tilePrefab);
				instance.transform.SetParent(parentGO.transform);
				instance.name = "MapTile_" + tile.Position;
				instance.transform.position = tile.CartesianPosition;
				tileObjects[tile.Index] = instance;
			}

			//put the following at the end of the start method (or in its own method called after map creation)
			//Camera.main.transform.position = new Vector3(hexMap.MapSizeData.center.x, 4, hexMap.MapSizeData.center.z); // centers the camera and moves it 5 units above the XZ-plane
			Camera.main.orthographic = true; //for this example we use an orthographic camera.
			//Camera.main.transform.rotation = Quaternion.Euler(90, 0, 0); //rotates the camera to it looks at the XZ-plane
			Camera.main.orthographicSize = hexMap.MapSizeData.extents.z * 2 * 0.8f; // sets orthographic size of the camera.]																		//this does not account for aspect ratio but for our purposes it works good enough.
		}

		void Update()
		{
			if (!hexMouse.CursorIsOnMap)
			{
				return; // if we are not on the map we won't do anything so we can return
			}
			Vector3Int mouseTilePosition = hexMouse.TileCoord;
			//update the marker positions
			if (Input.GetMouseButtonDown(0)) // change a tile when clicked on it
			{
				if (selectedTileObject != null)
				{
					selectedTileObject.GetComponentInChildren<SpriteRenderer>().color = Color.white;
				}
				Tile<int> t = hexMap.TilesByPosition[mouseTilePosition]; //we select the tile our mouse is on
				int curValue = t.Data; //we grab the current value of the tile
				t.Data = ((curValue + 1) % 4); //we increment it and use modulo to keep it between 0 and 3
				tileObjects[t.Index].GetComponentInChildren<SpriteRenderer>().color = Color.red;
				selectedTileObject = tileObjects[t.Index];
			}
		}
	}
}
