using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexMapController : MonoBehaviour
	{
		[SerializeField]
		private HexMapRenderer mapRenderer;
		[SerializeField]
		private HexMouseAdapter mouseAdapter;
		public HexMouseAdapter MouseAdapter => mouseAdapter;
		
		[SerializeField]
		private MonoBehaviour[] visualLayerBehaviours;
		private IHexMapVisualLayer[] _visualLayers;
		
		private readonly HexMapContext _context = new HexMapContext();
		public HexMapContext Context => _context;
		
		public IReadOnlyDictionary<Vector3Int, HexTile> CellsByPosition => mapRenderer.CellsByPosition;

		// Fired after every full visual refresh — both on initial load and on any subsequent rebuild.
		public event Action OnMapUpdated;
		
		private void Awake()
		{
			_visualLayers = visualLayerBehaviours
				.Where(behaviour => behaviour is IHexMapVisualLayer)
				.OfType<IHexMapVisualLayer>()
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

			RefreshVisuals();
			OnMapUpdated?.Invoke();
		}

		public void RefreshVisuals()
		{
			for (int i = 0; i < _visualLayers.Length; i++)
			{
				_visualLayers[i].Apply(_context, mapRenderer.CellsByPosition);
			}
		}
	}
}
