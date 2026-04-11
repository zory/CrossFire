using System;
using CrossFire.Ships;

namespace CrossFire.App
{
	/// <summary>
	/// Complete data snapshot of a gameplay scene at a given point in time.
	/// Passed via <see cref="GameplaySceneRequest"/> when loading the scene from another
	/// scene or from code (main menu, tests, samples); also populated from the Inspector
	/// via <see cref="GameplayBootstrap"/> as the in-scene fallback.
	///
	/// Designed to grow into a full save/load record:
	/// <list type="bullet">
	///   <item>Ship roster (current)</item>
	///   <item>TODO: Bullets in flight — position, velocity, owner, lifetime remaining</item>
	///   <item>TODO: Mission objectives — state, timers, progress</item>
	///   <item>TODO: Any other runtime state required for save/restore</item>
	/// </list>
	/// </summary>
	[Serializable]
	public class GameplaySceneState
	{
		public ShipSpawnEntry[] Ships;
	}
}
