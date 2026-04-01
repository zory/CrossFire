using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public class TeamColorVisualLayer : MonoBehaviour, IHexMapVisualLayer
	{
		[SerializeField]
		private Color[] teamColors;

		public void Apply(HexMapContext context, IReadOnlyDictionary<Vector3Int, HexCell> cellsByPosition)
		{
			foreach (KeyValuePair<Vector3Int, HexCell> pair in cellsByPosition)
			{
				Vector3Int tilePosition = pair.Key;
				HexCell hexCell = pair.Value;

				Color color = Color.white;
				if (context.Model.TilesToTeamIds.TryGetValue(tilePosition, out int teamId))
				{
					if (teamId >= 0 && teamId < teamColors.Length)
					{
						color = teamColors[teamId];
					}
				}

				hexCell.SpriteRenderer.color = color;
			}
		}
	}
}
