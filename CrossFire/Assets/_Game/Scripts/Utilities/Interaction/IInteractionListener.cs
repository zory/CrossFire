namespace Core.Utilities
{
    public interface IInteractionListener
    {
        // Called by InteractionBus when any interaction event is raised.
        // Cast evt.Context to the specific context type you care about and ignore others.
        // Register in OnEnable, unregister in OnDisable to avoid stale listener references.
        void OnInteraction(InteractionEvent interactionEvent);
    }
}
