using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.UI
{
    // Singleton UI manager. Persists across scenes.
    // AppUIController registers panels here on startup.
    // Anything that needs to open or close a panel calls UIRoot.Instance.Show<T>() / Hide<T>().
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        // All panels are instantiated as children of this transform.
        // Assign the Root RectTransform inside the Canvas in the inspector.
        [SerializeField]
        private RectTransform _panelRoot;
        public RectTransform PanelRoot => _panelRoot;

        private readonly Dictionary<Type, IPanel> _panels = new Dictionary<Type, IPanel>();
        private readonly Dictionary<Type, int> _priorities = new Dictionary<Type, int>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Called by AppUIController during startup to make a panel available.
        // Priority controls render order: lower values are behind, higher values are in front.
        // Black edges overlay would use a high value; background panels use negative values.
        public void Register<T>(T panel, int priority = 0) where T : IPanel
        {
            _panels[typeof(T)] = panel;
            _priorities[typeof(T)] = priority;
            ApplyPanelOrder();
        }

        // Returns the registered panel of type T.
        public T Get<T>() where T : IPanel
        {
            if (_panels.TryGetValue(typeof(T), out IPanel panel))
            {
                return (T)panel;
            }

            Debug.LogWarning($"[UIRoot] No panel registered for type {typeof(T).Name}.");
            return default;
        }

        // Shows the panel of type T. Optional configure callback runs before OnShow,
        // useful for passing parameters (e.g. which mission to display).
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

        // Re-orders panel GameObjects under PanelRoot by priority (ascending = behind).
        private void ApplyPanelOrder()
        {
            List<(IPanel Panel, int Priority)> sorted = new List<(IPanel, int)>();

            foreach (KeyValuePair<Type, IPanel> kvp in _panels)
            {
                _priorities.TryGetValue(kvp.Key, out int priority);
                sorted.Add((kvp.Value, priority));
            }

            sorted.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].Panel is Component component)
                {
                    component.transform.SetSiblingIndex(i);
                }
            }
        }
    }
}
