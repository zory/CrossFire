namespace CrossFire.App
{
	/// <summary>
	/// Static holder for a pending gameplay scene state.
	/// Set this from any scene (main menu, map editor, test harness) before loading
	/// the Gameplay scene; <see cref="GameplayBootstrap"/> will consume it on Start.
	///
	/// If never set, <see cref="HasPendingRequest"/> is false and
	/// <see cref="GameplayBootstrap"/> falls back to its inspector-configured state.
	/// </summary>
	public static class GameplaySceneRequest
	{
		public static GameplaySceneState State { get; private set; }
		public static bool HasPendingRequest { get; private set; }

		public static void Set(GameplaySceneState state)
		{
			State = state;
			HasPendingRequest = true;
		}

		public static void Clear()
		{
			State = null;
			HasPendingRequest = false;
		}
	}
}
