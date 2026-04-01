using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexMapController : MonoBehaviour
	{
		public event Action OnStructureRebuilt;
		public event Action OnVisualsRefreshed;

		[SerializeField]
		private HexMapRenderer mapRenderer;
		[SerializeField]
		private HexMouseAdapter mouseAdapter;
		[SerializeField]
		private MonoBehaviour[] visualLayerBehaviours;

		private readonly HexMapContext _context = new HexMapContext();
		private IHexMapVisualLayer[] _visualLayers;

		public HexMapContext Context => _context;
		public IReadOnlyDictionary<Vector3Int, HexCell> CellsByPosition => mapRenderer.CellsByPosition;
		public HexMouseAdapter MouseAdapter => mouseAdapter;

		private void Awake()
		{
			_visualLayers = visualLayerBehaviours
				.Where(behaviour => behaviour is IHexMapVisualLayer)
				.Cast<IHexMapVisualLayer>()
				.ToArray();
		}

		public void SetModel(HexMapModel model)
		{
			HexMapModelOperations.Clear(_context.Model);

			foreach (KeyValuePair<Vector3Int, int> pair in model.Tiles)
			{
				_context.Model.Tiles[pair.Key] = pair.Value;
			}

			foreach (KeyValuePair<Vector3Int, int> pair in model.TilesToTeamIds)
			{
				_context.Model.TilesToTeamIds[pair.Key] = pair.Value;
			}
		}

		public void RebuildStructure()
		{
			HexMapModelOperations.ReindexTiles(_context.Model);
			_context.RebuildHexMap();

			if (mouseAdapter != null)
			{
				mouseAdapter.Bind(_context.HexMap);
			}

			if (mapRenderer != null)
			{
				mapRenderer.Render(_context);
			}

			OnStructureRebuilt?.Invoke();
			RefreshVisuals();
		}

		public void RefreshVisuals()
		{
			for (int i = 0; i < _visualLayers.Length; i++)
			{
				_visualLayers[i].Apply(_context, mapRenderer.CellsByPosition);
			}

			OnVisualsRefreshed?.Invoke();
		}
	}
}
