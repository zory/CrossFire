using Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CrossFire.App.UI
{
    // Tooltip that displays mission description.
    // Requires a GraphicRaycaster on the Canvas and a raycast-target graphic on this prefab
    // so pointer enter/exit events register correctly.
    public class MissionTooltipPopup : PopupBase, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private TMP_Text _descriptionText;

        public bool IsMouseOver { get; private set; }

        protected override void OnSpawn()
        {
            // Pivot at bottom-left so SetScreenPosition places that corner at the mouse position.
            ((RectTransform)transform).pivot = Vector2.zero;
        }

        public void SetDescription(string description)
        {
            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsMouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsMouseOver = false;
        }
    }
}
