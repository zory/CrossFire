using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	// Visual layer that renders a glowing outline on every hex cell that has a mission assigned.
	// Instantiates one outline prefab per mission tile, parented to the tile's transform so the
	// outline automatically follows it in world space.
	public class MissionOutlineVisualLayer : MonoBehaviour, IHexMapVisualLayer
	{
		[SerializeField]
		private GameObject outlinePrefab;

		private readonly Dictionary<Vector3Int, GameObject> _outlineInstances = new Dictionary<Vector3Int, GameObject>();

		public void Apply(HexMapContext context, IReadOnlyDictionary<Vector3Int, HexTile> cellsByPosition)
		{
			DestroyAllOutlines();

			foreach (KeyValuePair<Vector3Int, HexTile> pair in cellsByPosition)
			{
				Vector3Int tilePosition = pair.Key;
				HexTile hexTile = pair.Value;

				if (!context.Model.TilesToMissionIds.ContainsKey(tilePosition))
				{
					continue;
				}

				GameObject outlineInstance = Instantiate(outlinePrefab, hexTile.transform);
				outlineInstance.transform.localPosition = Vector3.zero;
				outlineInstance.transform.localRotation = Quaternion.identity;

				_outlineInstances[tilePosition] = outlineInstance;
			}
		}

		private void OnDestroy()
		{
			DestroyAllOutlines();
		}

		private void DestroyAllOutlines()
		{
			foreach (GameObject instance in _outlineInstances.Values)
			{
				if (instance != null)
				{
					Destroy(instance);
				}
			}

			_outlineInstances.Clear();
		}
	}
}
