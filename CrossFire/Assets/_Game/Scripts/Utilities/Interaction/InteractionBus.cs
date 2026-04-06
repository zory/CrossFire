using System.Collections.Generic;

namespace CrossFire.Utilities
{
    // Static event bus for interaction events (hover, click) across all game systems.
    // Any MonoBehaviour or system can register as a listener to receive all interaction events
    // and filter by context type. Detectors (per-domain MonoBehaviours) raise events here.
    public static class InteractionBus
    {
        private static readonly List<IInteractionListener> _listeners = new List<IInteractionListener>();

        // Snapshot buffer used during Raise to avoid mutation-during-iteration issues.
        private static readonly List<IInteractionListener> _dispatchSnapshot = new List<IInteractionListener>();

        public static void Register(IInteractionListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public static void Unregister(IInteractionListener listener)
        {
            _listeners.Remove(listener);
        }

        public static void Raise(InteractionEvent interactionEvent)
        {
            _dispatchSnapshot.Clear();
            _dispatchSnapshot.AddRange(_listeners);

            for (int i = 0; i < _dispatchSnapshot.Count; i++)
            {
                _dispatchSnapshot[i].OnInteraction(interactionEvent);
            }
        }
    }
}
