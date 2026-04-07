using System;
using System.Collections.Generic;
using Core.UI;
using UnityEngine;

namespace CrossFire.App.UI
{
    // Base class for scene-specific UI controllers.
    //
    // Unlike AppUIController (which is global and DontDestroyOnLoad), subclasses live
    // in a single scene. They register their panels into the global UIRoot on Start
    // and automatically unregister + destroy those panels when the scene unloads.
    //
    // Usage: override RegisterPanels() and call RegisterPanel<T>(prefab, priority) for
    // each panel this scene owns. UIRoot provides the canvas, priority ordering, and
    // Show/Hide API — no separate canvas needed per scene.
    public abstract class SceneUIController : MonoBehaviour
    {
        private readonly List<Action> _cleanupActions = new List<Action>();

        private void Start()
        {
            RegisterPanels();
        }

        // Override to call RegisterPanel<T>() for each panel this scene owns.
        protected abstract void RegisterPanels();

        // Registers a panel prefab with UIRoot and queues its cleanup for when this scene unloads.
        protected T RegisterPanel<T>(T prefab, int priority = 0) where T : PanelBase
        {
            T instance = UIRoot.Instance.Panels.RegisterFromPrefab(prefab, priority);
            _cleanupActions.Add(() => UIRoot.Instance.Panels.Unregister<T>());
            return instance;
        }

        protected virtual void OnDestroy()
        {
            if (UIRoot.Instance == null)
            {
                return;
            }

            foreach (Action cleanup in _cleanupActions)
            {
                cleanup();
            }
        }
    }
}
