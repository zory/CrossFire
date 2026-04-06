using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    // Singleton UI entry point. Persists across scenes.
    // Owns PanelLayer and PopupLayer — both operate under the same ContentRoot
    // and share the same priority ordering so panels and popups are sorted together.
    //
    // Call via UIRoot.Instance.Panels / UIRoot.Instance.Popups for full API,
    // or use the shorthand methods on UIRoot directly for the common cases.
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        // All panels and popups are instantiated as children of this transform.
        [SerializeField]
        private RectTransform _contentRoot;

        // Only needed when the Canvas render mode is Screen Space - Camera.
        // Leave null for Screen Space - Overlay.
        [SerializeField]
        private Camera _uiCamera;

        public PanelLayer Panels { get; private set; }
        public PopupLayer Popups { get; private set; }

        public RectTransform ContentRoot => _contentRoot;
        internal Camera UICamera => _uiCamera;

        private readonly Dictionary<GameObject, int> _priorities = new Dictionary<GameObject, int>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Panels = new PanelLayer(this);
            Popups = new PopupLayer(this);
        }

        // --- Shorthand endpoints ---

        public void Register<T>(T panel, int priority = 0) where T : IPanel
            => Panels.Register(panel, priority);

        public void Show<T>(Action<T> configure = null) where T : IPanel
            => Panels.Show(configure);

        public void Hide<T>() where T : IPanel
            => Panels.Hide<T>();

        public T Spawn<T>(T prefab, int priority = 0) where T : PopupBase
            => Popups.Spawn(prefab, priority);

        public T Spawn<T>(T prefab, Vector2 screenPosition, int priority = 0) where T : PopupBase
            => Popups.Spawn(prefab, screenPosition, priority);

        public void Despawn(PopupBase popup)
            => Popups.Despawn(popup);

        // --- Shared priority ordering ---

        // Called by PanelLayer and PopupLayer when an object enters the hierarchy.
        internal void TrackPriority(GameObject go, int priority)
        {
            _priorities[go] = priority;
            ApplyOrder();
        }

        // Called by PopupLayer when a popup is destroyed.
        internal void UntrackPriority(GameObject go)
        {
            _priorities.Remove(go);
        }

        private void ApplyOrder()
        {
            List<(Transform Transform, int Priority)> children = new List<(Transform, int)>();

            foreach (Transform child in _contentRoot)
            {
                _priorities.TryGetValue(child.gameObject, out int priority);
                children.Add((child, priority));
            }

            children.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Transform.SetSiblingIndex(i);
            }
        }
    }
}
