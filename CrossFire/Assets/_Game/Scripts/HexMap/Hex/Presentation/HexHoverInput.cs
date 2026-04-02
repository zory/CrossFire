using UnityEngine;

namespace CrossFire.HexMap
{
    // Reads mouse position from HexMouseAdapter each frame and forwards the hovered
    // tile coordinates to HexTileSelector. This is the only script that touches input;
    // HexTileSelector contains no input logic.
    public class HexHoverInput : MonoBehaviour
    {
        [SerializeField]
        private HexMapController mapController;

        [SerializeField]
        private HexTileSelector tileSelector;

        private void Update()
        {
            if (mapController == null || mapController.MouseAdapter == null || tileSelector == null)
            {
                return;
            }

            HexMouseAdapter mouse = mapController.MouseAdapter;

            if (!mouse.CursorOnMap)
            {
                tileSelector.ClearActiveTile();
                return;
            }

            tileSelector.SetActiveTile(mouse.TileCoords);
        }
    }
}