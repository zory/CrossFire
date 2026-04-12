namespace CrossFire.HexMap
{
	/// <summary>
	/// Implemented by hex map paint tools that consume mouse input.
	/// Used by <see cref="HexTileInteractionDetector"/> to suppress hover/click
	/// interactions while editing is active.
	/// </summary>
	public interface IHexEditingTool
	{
		bool IsEditing { get; }
	}
}
