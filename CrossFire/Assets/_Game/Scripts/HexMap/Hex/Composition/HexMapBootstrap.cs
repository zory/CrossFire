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
				new TeamsLayerSerializer(),
				new MissionsLayerSerializer());
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

		public void Load(string fileNameToLoad)
		{
			HexMapModel loadedModel = _loadPipeline.Load(fileNameToLoad);
			mapController.SetModel(loadedModel);
			mapController.RebuildStructure();
		}

		public void Save(string fileNameToSave)
		{
			_loadPipeline.Save(fileName, mapController.Context.Model);
		}
	}
}