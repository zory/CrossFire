using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexMissionPaintTool : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private bool enableEditing = true;
		[SerializeField]
		private int missionId;
		[SerializeField]
		private bool rightClickClearsMission = true;

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
				if (HexMapModelOperations.SetMission(model, tilePosition, missionId))
				{
					mapController.RefreshVisuals();
				}
			}

			if (rightClickClearsMission && Input.GetMouseButtonDown(1))
			{
				if (HexMapModelOperations.ClearMission(model, tilePosition))
				{
					mapController.RefreshVisuals();
				}
			}
		}
	}
}
