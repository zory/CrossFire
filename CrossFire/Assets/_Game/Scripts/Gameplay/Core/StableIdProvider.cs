namespace CrossFire.Core
{
	/// <summary>
	/// Shared counter that issues unique <see cref="StableId"/> values across every entity
	/// type that carries the component (ships, bullets, etc.).
	/// </summary>
	/// <remarks>
	/// The counter is static and resets to zero on domain reload (i.e. each Play-mode
	/// session in the Editor starts fresh). IDs are unique within a session, not across
	/// sessions — which is all that runtime lookup code requires.
	/// </remarks>
	public static class StableIdProvider
	{
		private static int _nextId;

		public static int Next() => _nextId++;

		/// <summary>
		/// Returns the next ID that would be issued without consuming it.
		/// Used by save/load to snapshot the counter state.
		/// </summary>
		public static int Peek() => _nextId;

		/// <summary>
		/// Restores the counter to a previously saved value.
		/// Call after loading a simulation snapshot so future spawns
		/// do not collide with IDs already present in the restored world.
		/// </summary>
		public static void Restore(int nextId) => _nextId = nextId;

		/// <summary>
		/// Resets the counter to zero. Call this at the start of each test that
		/// requires predictable ID values.
		/// </summary>
		public static void Reset() => _nextId = 0;
	}
}
