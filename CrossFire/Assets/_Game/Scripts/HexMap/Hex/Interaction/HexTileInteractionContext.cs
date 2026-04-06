using CrossFire.Utilities;
using UnityEngine;

namespace CrossFire.HexMap
{
    public struct HexTileInteractionContext : IInteractionContext
    {
        // Cube coordinates of the tile.
        public Vector3Int TilePosition;
        // -1 if the tile has no mission assigned.
        public int MissionId;
        // -1 if the tile has no team assigned.
        public int TeamId;
    }
}
