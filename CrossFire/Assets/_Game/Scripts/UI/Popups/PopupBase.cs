using UnityEngine;

namespace CrossFire.UI
{
    // Base class for all spawnable UI elements (tooltips, dialogs, notifications).
    // Popups differ from panels: they can have multiple simultaneous instances and
    // are created/destroyed on demand rather than pre-registered at startup.
    public abstract class PopupBase : MonoBehaviour
    {
        // Called by PopupLayer immediately after instantiation.
        protected virtual void OnSpawn() { }

        // Called by PopupLayer just before destruction.
        protected virtual void OnDespawn() { }

        internal void NotifySpawned() => OnSpawn();
        internal void NotifyDespawned() => OnDespawn();
    }
}
