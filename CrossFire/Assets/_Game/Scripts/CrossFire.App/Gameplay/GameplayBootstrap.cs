using CrossFire.Ships;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Entry point for the Gameplay scene.
	/// On Start it resolves a <see cref="GameplaySceneState"/> — either from a pending
	/// <see cref="GameplaySceneRequest"/> (set by the calling scene) or from the
	/// inspector-configured <see cref="_defaultState"/> fallback — and spawns all ships.
	///
	/// The same state object will later carry bullets in flight, mission objectives, and
	/// any other runtime state needed for save/load or scene restoration.
	/// </summary>
	public class GameplayBootstrap : MonoBehaviour
	{
		[SerializeField]
		private GameplaySceneState _defaultState;

		private void Start()
		{
			GameplaySceneState state;

			if (GameplaySceneRequest.HasPendingRequest)
			{
				state = GameplaySceneRequest.State;
				GameplaySceneRequest.Clear();
			}
			else
			{
				state = _defaultState;
			}

			if (state == null || state.Ships == null)
			{
				return;
			}

			foreach (ShipSpawnEntry entry in state.Ships)
			{
				ShipSpawner.Spawn(entry.Type, entry.Team, entry.Pose);
			}
		}
	}
}
