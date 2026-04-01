using UnityEngine;
using Wunderwunsch.HexMapLibrary;
using Wunderwunsch.HexMapLibrary.Generic;

namespace CrossFire.HexMap
{
	public class HexMapGameBootstrap : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private bool generateRectangularMapOnStart;
		[SerializeField]
		private Vector2Int mapSize = new Vector2Int(13, 13);
		[SerializeField]
		private string fileName = "SavedMap";
		[SerializeField]
		private bool loadFromSaveOnStart = true;

		private HexMapLoadPipeline _loadPipeline;

		private void Awake()
		{
			_loadPipeline = new HexMapLoadPipeline(
				new BaseTilesLayerSerializer(),
				new TeamsLayerSerializer());
		}

		private void Start()
		{
			if (mapController == null)
			{
				return;
			}

			if (loadFromSaveOnStart)
			{
				HexMapModel loadedModel = _loadPipeline.Load(fileName);
				mapController.SetModel(loadedModel);
			}
			else if (generateRectangularMapOnStart)
			{
				GenerateRectangularMap();
			}

			mapController.RebuildStructure();
			FitOrthographicCamera();
		}

		private void GenerateRectangularMap()
		{
			HexMapModel model = new HexMapModel();
			HexMap<int, bool> generatedMap = new HexMap<int, bool>(HexMapBuilder.CreateRectangularShapedMap(mapSize), null);

			foreach (Tile<int> tile in generatedMap.Tiles)
			{
				model.Tiles[tile.Position] = tile.Index;
			}

			mapController.SetModel(model);
		}

		private void FitOrthographicCamera()
		{
			if (Camera.main == null || mapController.Context.HexMap == null)
			{
				return;
			}

			Camera.main.orthographic = true;
			Camera.main.orthographicSize = mapController.Context.HexMap.MapSizeData.extents.z * 2f * 0.8f;
		}
	}
}
