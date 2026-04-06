namespace Core.Utilities
{
    // Marker interface for interaction event payloads.
    // Implement this on any struct or class that carries context for a specific interactable type
    // (e.g. HexTileInteractionContext, ShipInteractionContext).
    public interface IInteractionContext { }
}
