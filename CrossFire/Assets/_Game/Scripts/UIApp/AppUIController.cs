using CrossFire.UI;
using UnityEngine;

namespace CrossFire.App.UI
{
    // Single wiring point for all UI panels. Lives on the same GameObject as UIRoot.
    // Drag panel prefabs into the inspector slots. On Awake each prefab is instantiated
    // under UIRoot.PanelRoot and registered with a render priority.
    //
    // Priority convention:
    //   negative  — background / underlays
    //   0         — normal panels
    //   positive  — overlays and always-on chrome (black edges: very high)
    //
    // Empty prefab slots are silently skipped — the panel simply won't be available.
    public class AppUIController : MonoBehaviour
    {
        private const int PRIORITY_BLACK_EDGES = 1000;

        [Header("Always-on Chrome")]
        [SerializeField]
        private BlackEdgesPanel _blackEdgesPanelPrefab;

        // Start runs after all Awakes complete, so UIRoot.Instance is guaranteed to be set.
        private void Start()
        {
            RegisterPanel(_blackEdgesPanelPrefab, PRIORITY_BLACK_EDGES);

            ShowAlwaysOnPanels();
        }

        private void ShowAlwaysOnPanels()
        {
            UIRoot.Instance.Show<BlackEdgesPanel>();
        }

        private void RegisterPanel<T>(T prefab, int priority = 0) where T : PanelBase
        {
            if (prefab == null)
            {
                return;
            }

            T instance = Instantiate(prefab, UIRoot.Instance.PanelRoot);
            instance.Hide();
            UIRoot.Instance.Register(instance, priority);
        }
    }
}
