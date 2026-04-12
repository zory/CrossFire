using Core.UI;
using Core.Utilities;
using CrossFire.App;
using CrossFire.HexMap;
using UnityEngine;

namespace CrossFire.App.UI
{
    // Listens for hex tile hover events and drives the charge-then-tooltip flow:
    //   1. Hover begins → progress indicator appears at cursor, fills over _hoverDuration.
    //   2. Charge interrupted (cursor leaves tile) → indicator despawned, timer reset.
    //   3. Charge completes → indicator despawned, mission tooltip spawned at cursor position.
    //   4. Tooltip stays open while cursor is on the original tile OR on the tooltip itself.
    //   5. Cursor moves anywhere else → tooltip despawned.
    public class MissionHoverController : MonoBehaviour, IInteractionListener
    {
        [SerializeField]
        private HexMapController _mapController;

        [SerializeField]
        private HoverProgressPopup _progressPrefab;

        [SerializeField]
        private MissionTooltipPopup _tooltipPrefab;

        [SerializeField]
        private float _hoverDuration = 1.5f;

        private enum HoverState { Idle, Charging, TooltipOpen }

        private HoverState _state = HoverState.Idle;
        private Vector3Int _activeTilePosition;
        private int _activeMissionId;
        private float _hoverTimer;
        private HoverProgressPopup _progressInstance;
        private MissionTooltipPopup _tooltipInstance;

        private void OnEnable()
        {
            InteractionBus.Register(this);
        }

        private void OnDisable()
        {
            InteractionBus.Unregister(this);

            if (_state == HoverState.Charging)
            {
                CancelCharging();
            }
            else if (_state == HoverState.TooltipOpen)
            {
                CloseTooltip();
            }
        }

        public void OnInteraction(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Context is not HexTileInteractionContext ctx)
            {
                return;
            }

            if (interactionEvent.Type == InteractionEventType.HoverEnter)
            {
                HandleHoverEnter(ctx);
            }
            else if (interactionEvent.Type == InteractionEventType.HoverExit)
            {
                HandleHoverExit();
            }
        }

        private void Update()
        {
            if (_state == HoverState.Charging)
            {
                UpdateCharging();
            }
            else if (_state == HoverState.TooltipOpen)
            {
                UpdateTooltipOpen();
            }
        }

        private void HandleHoverEnter(HexTileInteractionContext ctx)
        {
            // Tooltip takes priority over tiles beneath it — ignore tile events while
            // the cursor is inside the tooltip bounds.
            if (_state == HoverState.TooltipOpen && IsMouseOverTooltip())
            {
                return;
            }

            if (_state == HoverState.TooltipOpen)
            {
                CloseTooltip();
            }
            else if (_state == HoverState.Charging)
            {
                CancelCharging();
            }

            _activeTilePosition = ctx.TilePosition;
            _activeMissionId = ctx.MissionId;
            _hoverTimer = 0f;
            _state = HoverState.Charging;
            _progressInstance = UIRoot.Instance.Popups.Spawn(_progressPrefab);
        }

        private void HandleHoverExit()
        {
            if (_state == HoverState.Charging)
            {
                CancelCharging();
            }
            // TooltipOpen: don't close immediately — UpdateTooltipOpen checks every frame
            // whether cursor is still on the tile or on the tooltip itself.
        }

        private void UpdateCharging()
        {
            _hoverTimer += Time.deltaTime;

            _progressInstance.SetProgress(_hoverTimer / _hoverDuration);
            UIRoot.Instance.Popups.SetScreenPosition(_progressInstance, Input.mousePosition);

            if (_hoverTimer >= _hoverDuration)
            {
                CompleteHover();
            }
        }

        private void UpdateTooltipOpen()
        {
            bool mouseOnOriginalTile = _mapController.MouseAdapter.CursorOnMap
                && _mapController.MouseAdapter.TileCoords == _activeTilePosition;

            bool mouseOnTooltip = _tooltipInstance != null && _tooltipInstance.IsMouseOver;

            if (!mouseOnOriginalTile && !mouseOnTooltip)
            {
                CloseTooltip();
            }
        }

        private void CompleteHover()
        {
            UIRoot.Instance.Popups.Despawn(_progressInstance);
            _progressInstance = null;

            MissionData missionData = MissionSaveData.LoadMetadata(_activeMissionId);

            _tooltipInstance = UIRoot.Instance.Popups.Spawn(_tooltipPrefab, Input.mousePosition);
            _tooltipInstance.SetDescription(missionData.Description);

            _state = HoverState.TooltipOpen;
        }

        private void CancelCharging()
        {
            UIRoot.Instance.Popups.Despawn(_progressInstance);
            _progressInstance = null;
            _hoverTimer = 0f;
            _state = HoverState.Idle;
        }

        private void CloseTooltip()
        {
            UIRoot.Instance.Popups.Despawn(_tooltipInstance);
            _tooltipInstance = null;
            _state = HoverState.Idle;
        }

        // Direct bounds check rather than relying on EventSystem pointer events,
        // which share the same frame as Update and have undefined execution order.
        private bool IsMouseOverTooltip()
        {
            if (_tooltipInstance == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)_tooltipInstance.transform,
                Input.mousePosition,
                UIRoot.Instance.UICamera
            );
        }
    }
}
