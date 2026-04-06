using UnityEngine;

namespace CrossFire.UI
{
    // Base class for all UI panels. Handles show/hide via GameObject activation
    // and exposes OnShow/OnHide hooks for subclass logic.
    public abstract class PanelBase : MonoBehaviour, IPanel
    {
        public bool IsVisible => gameObject.activeSelf;

        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
        }

        // Override to run logic when this panel becomes visible.
        protected virtual void OnShow() { }

        // Override to run logic when this panel is hidden.
        protected virtual void OnHide() { }
    }
}
