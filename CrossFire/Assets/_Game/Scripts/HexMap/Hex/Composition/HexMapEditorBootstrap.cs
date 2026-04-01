using UnityEditor;
using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexMapEditorBootstrap : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private string fileName = "SavedMap";
		[SerializeField]
		private bool loadOnStart = true;
		[SerializeField]
		private bool saveNow;
		[SerializeField]
		private bool loadNow;

		private HexMapLoadPipeline _loadPipeline;

		private void Awake()
		{
			_loadPipeline = new HexMapLoadPipeline(
				new BaseTilesLayerSerializer(),
				new TeamsLayerSerializer());
		}

		private void Start()
		{
			if (loadOnStart)
			{
				Load();
			}
			else if (mapController != null)
			{
				mapController.RebuildStructure();
			}
		}

		private void Update()
		{
			if (saveNow)
			{
				saveNow = false;
				Save();
			}

			if (loadNow)
			{
				loadNow = false;
				Load();
			}
		}

		public void Save()
		{
			if (mapController == null)
			{
				return;
			}

			_loadPipeline.Save(fileName, mapController.Context.Model);
		}

		public void Load()
		{
			if (mapController == null)
			{
				return;
			}

			HexMapModel loadedModel = _loadPipeline.Load(fileName);
			mapController.SetModel(loadedModel);
			mapController.RebuildStructure();
		}
	}
}
