using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.UI
{
    // Manages transient popups — spawned on demand, multiple instances allowed.
    // Owned by UIRoot; do not instantiate directly.
    public class PopupLayer
    {
        private readonly UIRoot _root;
        private readonly List<PopupBase> _activePopups = new List<PopupBase>();

        internal PopupLayer(UIRoot root)
        {
            _root = root;
        }

        public T Spawn<T>(T prefab, int priority = 0) where T : PopupBase
        {
            T instance = Object.Instantiate(prefab, _root.ContentRoot);
            _root.TrackPriority(instance.gameObject, priority);
            _activePopups.Add(instance);
            instance.NotifySpawned();
            return instance;
        }

        public T Spawn<T>(T prefab, Vector2 screenPosition, int priority = 0) where T : PopupBase
        {
            T instance = Spawn(prefab, priority);
            SetScreenPosition(instance, screenPosition);
            return instance;
        }

        public void Despawn(PopupBase popup)
        {
            if (popup == null || !_activePopups.Remove(popup))
            {
                return;
            }

            _root.UntrackPriority(popup.gameObject);
            popup.NotifyDespawned();
            Object.Destroy(popup.gameObject);
        }

        public void DespawnAll()
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                Despawn(_activePopups[i]);
            }
        }

        public void SetScreenPosition(PopupBase popup, Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root.ContentRoot,
                screenPosition,
                _root.UICamera,
                out Vector2 localPoint
            );

            ((RectTransform)popup.transform).anchoredPosition = localPoint;
        }
    }
}
