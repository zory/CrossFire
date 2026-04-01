using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexMapBootstrap : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private string fileName = "SavedMap";
		[SerializeField]
		private bool loadOnStart = true;

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

			// A pending scene request overrides the inspector settings.
			string fileNameToLoad = fileName;
			bool shouldLoadFromFile = loadOnStart;

			if (HexMapSceneRequest.HasPendingRequest)
			{
				fileNameToLoad = HexMapSceneRequest.FileName;
				shouldLoadFromFile = true;
				HexMapSceneRequest.Clear();
			}

			if (shouldLoadFromFile)
			{
				Load(fileNameToLoad);
			}
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

		public void Load(string fileNameToLoad)
		{
			HexMapModel loadedModel = _loadPipeline.Load(fileNameToLoad);
			mapController.SetModel(loadedModel);
			
			mapController.RebuildStructure();
			FitOrthographicCamera();
		}

		public void Save(string fileNameToSave)
		{
			_loadPipeline.Save(fileName, mapController.Context.Model);
		}
	}
}