using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Core.UI
{
    // Attach to the persistent EventSystem inside the UI prefab.
    // On each scene load it destroys any scene-local EventSystems so there is always
    // exactly one active. Test scenes can carry their own EventSystem for standalone
    // use — it will be cleaned up automatically when the full UI is present.
    public class EventSystemGuard : MonoBehaviour
    {
        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Start runs after all Awakes complete, so all scene EventSystems are
        // initialized and visible to FindObjectsByType before we destroy duplicates.
        private void Start()
        {
            DestroyDuplicates();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DestroyDuplicates();
        }

        private void DestroyDuplicates()
        {
            EventSystem[] allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

            foreach (EventSystem eventSystem in allEventSystems)
            {
                if (eventSystem.gameObject != gameObject)
                {
                    Destroy(eventSystem.gameObject);
                }
            }
        }
    }
}
