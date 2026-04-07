using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    // Manages singleton panels — one registered instance per type, shown and hidden in place.
    // Owned by UIRoot; do not instantiate directly.
    public class PanelLayer
    {
        private readonly UIRoot _root;
        private readonly Dictionary<Type, IPanel> _panels = new Dictionary<Type, IPanel>();

        internal PanelLayer(UIRoot root)
        {
            _root = root;
        }

        // Instantiates a prefab under ContentRoot, hides it, and registers it.
        // Use this from AppUIController instead of instantiating manually.
        public T RegisterFromPrefab<T>(T prefab, int priority = 0) where T : PanelBase
        {
            if (prefab == null)
            {
                return null;
            }

            T instance = UnityEngine.Object.Instantiate(prefab, _root.ContentRoot);
            instance.Hide();
            Register(instance, priority);
            return instance;
        }

        public void Register<T>(T panel, int priority = 0) where T : IPanel
        {
            _panels[typeof(T)] = panel;

            if (panel is Component component)
            {
                _root.TrackPriority(component.gameObject, priority);
            }
        }

        public T Get<T>() where T : IPanel
        {
            if (_panels.TryGetValue(typeof(T), out IPanel panel))
            {
                return (T)panel;
            }

            Debug.LogWarning($"[UIRoot] No panel registered for type {typeof(T).Name}.");
            return default;
        }

        // configure runs before Show, useful for passing data into the panel.
        public void Show<T>(Action<T> configure = null) where T : IPanel
        {
            T panel = Get<T>();
            if (panel == null)
            {
                return;
            }

            configure?.Invoke(panel);
            panel.Show();
        }

        public void Hide<T>() where T : IPanel
        {
            T panel = Get<T>();
            if (panel == null)
            {
                return;
            }

            panel.Hide();
        }

        // Removes the panel from the registry, untracks its priority, and destroys its GameObject.
        // Use this from scene-specific UI controllers in OnDestroy to clean up scene-owned panels.
        public void Unregister<T>() where T : IPanel
        {
            if (!_panels.TryGetValue(typeof(T), out IPanel panel))
            {
                return;
            }

            _panels.Remove(typeof(T));

            if (panel is Component component)
            {
                _root.UntrackPriority(component.gameObject);
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }
    }
}
