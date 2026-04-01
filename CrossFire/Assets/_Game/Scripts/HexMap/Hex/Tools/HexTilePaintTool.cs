using UnityEngine;

namespace CrossFire.HexMap
{
	public class HexTilePaintTool : MonoBehaviour
	{
		[SerializeField]
		private HexMapController mapController;
		[SerializeField]
		private bool enableEditing = true;

		private void Update()
		{
			if (!enableEditing || mapController == null || mapController.MouseAdapter == null)
			{
				return;
			}
			
			Vector3Int tilePosition = mapController.MouseAdapter.TileCoords;
			HexMapModel model = mapController.Context.Model;

			if (Input.GetMouseButtonDown(0))
			{
				if (HexMapModelOperations.AddTile(model, tilePosition))
				{
					mapController.RebuildStructure();
				}
			}

			if (Input.GetMouseButtonDown(1))
			{
				if (HexMapModelOperations.RemoveTile(model, tilePosition))
				{
					mapController.RebuildStructure();
				}
			}
		}
	}
}
