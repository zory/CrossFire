using UnityEngine;
using Wunderwunsch.HexMapLibrary;

namespace CrossFire.HexMap
{
    // Manages the lifecycle and position of the selectable tile marker prefab.
    // Call SetActiveTile() with a cube-coordinate position:
    //   - If the tile exists in the map, the marker is created (or moved) there.
    //   - If the tile does not exist, any existing marker is destroyed.
    // Call ClearActiveTile() to unconditionally destroy the marker.
    public class HexTileSelector : MonoBehaviour
    {
        [SerializeField]
        private HexMapController mapController;

        [SerializeField]
        private GameObject selectorPrefab;

        private HexSelectableTile _activeInstance;
        private Vector3Int _activePosition;
        private bool _hasActivePosition;

        public bool HasActivePosition => _hasActivePosition;
        public Vector3Int ActivePosition => _activePosition;

        // Sets the active tile to the given cube-coordinate position.
        // Creates the marker if the tile exists; destroys it otherwise.
        public void SetActiveTile(Vector3Int tileCoords)
        {
            bool tileExists = mapController != null && mapController.CellsByPosition.ContainsKey(tileCoords);

            if (!tileExists)
            {
                ClearActiveTile();
                return;
            }

            _activePosition = tileCoords;
            _hasActivePosition = true;

            if (_activeInstance == null)
            {
                _activeInstance = CreateInstance();
            }

            Vector3 worldPosition = HexConverter.TileCoordToCartesianCoord(tileCoords, 0);
            _activeInstance.transform.position = worldPosition;
        }

        // Destroys the marker and clears tracked position.
        public void ClearActiveTile()
        {
            if (_activeInstance != null)
            {
                Destroy(_activeInstance.gameObject);
                _activeInstance = null;
            }

            _hasActivePosition = false;
        }

        private HexSelectableTile CreateInstance()
        {
            if (selectorPrefab == null)
            {
                Debug.LogWarning("HexTileSelector: selectorPrefab is not assigned.", this);
                return null;
            }

            GameObject instance = Instantiate(selectorPrefab, transform);
            return instance.GetComponent<HexSelectableTile>();
        }
    }
}