namespace CrossFire.HexMap
{
	/// <summary>
	/// Static store for passing map load parameters between scenes.
	/// Set before loading a scene; the game bootstrap reads and clears it on startup.
	/// </summary>
	public static class HexMapSceneRequest
	{
		public static string FileName { get; private set; }
		public static bool HasPendingRequest { get; private set; }

		public static void Set(string fileName)
		{
			FileName = fileName;
			HasPendingRequest = true;
		}

		public static void Clear()
		{
			FileName = null;
			HasPendingRequest = false;
		}
	}
}