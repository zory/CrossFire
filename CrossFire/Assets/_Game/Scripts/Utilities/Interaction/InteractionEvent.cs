using UnityEngine;

namespace Core.Utilities
{
    public struct InteractionEvent
    {
        public InteractionEventType Type;
        // Null when no payload is available (e.g. hover exit after source is destroyed).
        public IInteractionContext Context;
        public GameObject Source;
    }
}
