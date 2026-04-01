using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexTeamPaintTool : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private int teamId;
		[SerializeField]
		private bool enableEditing = true;
		[SerializeField]
		private bool rightClickClearsTeam = true;

		private void Update()
		{
			if (!enableEditing || mapController == null || mapController.MouseAdapter == null)
			{
				return;
			}

			if (!mapController.MouseAdapter.CursorOnMap)
			{
				return;
			}

			Vector3Int tilePosition = mapController.MouseAdapter.TileCoords;
			HexMapModel model = mapController.Context.Model;

			if (Input.GetMouseButtonDown(0))
			{
				if (HexMapModelOperations.SetTeam(model, tilePosition, teamId))
				{
					mapController.RefreshVisuals();
				}
			}

			if (rightClickClearsTeam && Input.GetMouseButtonDown(1))
			{
				if (HexMapModelOperations.ClearTeam(model, tilePosition))
				{
					mapController.RefreshVisuals();
				}
			}
		}
	}
}
