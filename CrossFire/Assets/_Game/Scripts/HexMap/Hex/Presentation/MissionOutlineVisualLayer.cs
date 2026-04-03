using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	// Visual layer that renders a glowing outline on every hex cell that has a mission assigned.
	// The outline pulses between a darkened and a lightened variant of that tile's team color.
	// If the tile has no team, the outline uses the default colors baked into the prefab.
	public class MissionOutlineVisualLayer : MonoBehaviour, IHexMapVisualLayer
	{
		[SerializeField]
		private GameObject outlinePrefab;

		// Must match the array in TeamColorVisualLayer — index 0 = team 0, etc.
		[SerializeField]
		private Color[] teamColors;

		[SerializeField]
		[Range(0f, 1f)]
		private float darkenAmount = 0.35f;

		[SerializeField]
		[Range(0f, 1f)]
		private float lightenAmount = 0.35f;

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

				// Tint the animator toward the owning team's color if one is assigned.
				HexOutlineAnimator animator = outlineInstance.GetComponent<HexOutlineAnimator>();
				if (animator != null && context.Model.TilesToTeamIds.TryGetValue(tilePosition, out int teamId))
				{
					if (teamId >= 0 && teamId < teamColors.Length)
					{
						Color teamColor = teamColors[teamId];
						Color darker  = Color.Lerp(teamColor, Color.black, darkenAmount);
						Color lighter = Color.Lerp(teamColor, Color.white, lightenAmount);
						animator.SetColors(darker, lighter);
					}
				}

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
