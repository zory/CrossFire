using Core.Utilities;
using UnityEngine;

namespace CrossFire.HexMap
{
	// Detects hover and click interactions on hex tiles that have a mission assigned.
	// Place on any GameObject in the HexMap scene alongside HexMapController.
	// Raises InteractionBus events with HexTileInteractionContext as payload.
	// Extend the interactable filter (_hasMission check) when other tile types become selectable.
	//
	// Assign any active paint tools to _editingTools — interactions are suppressed while
	// any tool reports IsEditing == true, preventing edit clicks from also firing game events.
	public class HexTileInteractionDetector : MonoBehaviour
	{
		[SerializeField]
		private HexMapController _mapController;

		[SerializeField]
		private MonoBehaviour[] _editingTools;

		private Vector3Int _hoveredTilePosition;
		private bool _isHoveringInteractableTile;

		private void Update()
		{
			if (_mapController == null)
			{
				return;
			}

			if (IsAnyToolEditing())
			{
				// Cancel any in-progress hover so the tooltip doesn't get stuck open.
				if (_isHoveringInteractableTile)
				{
					HexMapModel model = _mapController.Context.Model;
					RaiseEvent(InteractionEventType.HoverExit, model, _hoveredTilePosition);
					_isHoveringInteractableTile = false;
				}
				return;
			}

			HexMouseAdapter mouseAdapter = _mapController.MouseAdapter;
			HexMapModel hexMapModel = _mapController.Context.Model;

			bool cursorOnMap = mouseAdapter.CursorOnMap;
			Vector3Int currentTilePosition = mouseAdapter.TileCoords;
			bool currentTileIsInteractable = cursorOnMap && IsInteractable(hexMapModel, currentTilePosition);

			HandleHoverTransition(hexMapModel, currentTilePosition, currentTileIsInteractable);
			HandleClick(hexMapModel, currentTilePosition, currentTileIsInteractable);
		}

		private bool IsAnyToolEditing()
		{
			if (_editingTools == null)
			{
				return false;
			}

			foreach (MonoBehaviour tool in _editingTools)
			{
				if (tool is IHexEditingTool editingTool && editingTool.IsEditing)
				{
					return true;
				}
			}
			return false;
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
