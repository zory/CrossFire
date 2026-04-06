using Core.Utilities;
using UnityEngine;

namespace CrossFire.HexMap
{
    // Detects hover and click interactions on hex tiles that have a mission assigned.
    // Place on any GameObject in the HexMap scene alongside HexMapController.
    // Raises InteractionBus events with HexTileInteractionContext as payload.
    // Extend the interactable filter (_hasMission check) when other tile types become selectable.
    public class HexTileInteractionDetector : MonoBehaviour
    {
        [SerializeField]
        private HexMapController _mapController;

        private Vector3Int _hoveredTilePosition;
        private bool _isHoveringInteractableTile;

        private void Update()
        {
            if (_mapController == null)
            {
                return;
            }

            HexMouseAdapter mouseAdapter = _mapController.MouseAdapter;
            HexMapModel model = _mapController.Context.Model;

            bool cursorOnMap = mouseAdapter.CursorOnMap;
            Vector3Int currentTilePosition = mouseAdapter.TileCoords;
            bool currentTileIsInteractable = cursorOnMap && IsInteractable(model, currentTilePosition);

            HandleHoverTransition(model, currentTilePosition, currentTileIsInteractable);
            HandleClick(model, currentTilePosition, currentTileIsInteractable);
        }

        private void HandleHoverTransition(HexMapModel model, Vector3Int currentTilePosition, bool currentTileIsInteractable)
        {
            bool tileChanged = currentTilePosition != _hoveredTilePosition;

            if (_isHoveringInteractableTile && (!currentTileIsInteractable || tileChanged))
            {
                RaiseEvent(InteractionEventType.HoverExit, model, _hoveredTilePosition);
                _isHoveringInteractableTile = false;
            }

            if (currentTileIsInteractable && (!_isHoveringInteractableTile || tileChanged))
            {
                _hoveredTilePosition = currentTilePosition;
                _isHoveringInteractableTile = true;
                RaiseEvent(InteractionEventType.HoverEnter, model, _hoveredTilePosition);
            }
        }

        private void HandleClick(HexMapModel model, Vector3Int currentTilePosition, bool currentTileIsInteractable)
        {
            if (currentTileIsInteractable && Input.GetMouseButtonDown(0))
            {
                RaiseEvent(InteractionEventType.Click, model, currentTilePosition);
            }
        }

        private static bool IsInteractable(HexMapModel model, Vector3Int tilePosition)
        {
            return model.TilesToMissionIds.ContainsKey(tilePosition);
        }

        private static void RaiseEvent(InteractionEventType eventType, HexMapModel model, Vector3Int tilePosition)
        {
            if (!model.TilesToMissionIds.TryGetValue(tilePosition, out int missionId))
            {
                missionId = -1;
            }

            if (!model.TilesToTeamIds.TryGetValue(tilePosition, out int teamId))
            {
                teamId = -1;
            }

            HexTileInteractionContext context = new HexTileInteractionContext
            {
                TilePosition = tilePosition,
                MissionId = missionId,
                TeamId = teamId
            };
            
            InteractionBus.Raise(new InteractionEvent
            {
                Type = eventType,
                Context = context,
                Source = null
            });
        }
    }
}
