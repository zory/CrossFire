using System.Collections;
using CrossFire.Ships;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Entry point for the Gameplay scene.
	/// On Start it resolves a <see cref="GameplaySceneState"/> — either from a pending
	/// <see cref="GameplaySceneRequest"/> (set by the calling scene) or from the
	/// inspector-configured <see cref="_defaultState"/> fallback — and initialises the world.
	///
	/// If the state carries a <see cref="GameplaySceneState.MissionId"/>, the saved simulation
	/// snapshot for that mission is restored. Because the prefab registry lives in a subscene
	/// that loads asynchronously, the restore is deferred in a coroutine until the registry
	/// is available. If no snapshot exists, falls back to spawning the
	/// <see cref="GameplaySceneState.Ships"/> list.
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

			if (state == null)
			{
				return;
			}

			if (state.MissionId > 0)
			{
				StartCoroutine(RestoreMissionWhenReady(state.MissionId));
				return;
			}

			SpawnShips(state);
		}

		/// <summary>
		/// Waits until the ship prefab registry is populated by the subscene,
		/// then restores the simulation snapshot for the given mission.
		/// </summary>
		private static IEnumerator RestoreMissionWhenReady(int missionId)
		{
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
			{
				Debug.LogWarning("[GameplayBootstrap] No active ECS world — cannot restore mission.");
				yield break;
			}

			// Poll each frame until the subscene has streamed in the prefab registry.
			EntityManager em = world.EntityManager;
			while (true)
			{
				using EntityQuery registryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<ShipPrefabEntry>());
				if (!registryQuery.IsEmpty)
				{
					break;
				}
				yield return null;
			}

			GameplaySimulationSnapshot snapshot = MissionSaveData.LoadSimulation(missionId);
			if (snapshot == null)
			{
				Debug.Log($"[GameplayBootstrap] No simulation save for mission {missionId} — starting empty.");
				yield break;
			}

			GameplaySimulationSerializer.RestoreSnapshot(snapshot, em);
			Debug.Log($"[GameplayBootstrap] Restored mission {missionId}: " +
			          $"{snapshot.Ships?.Length ?? 0} ships, {snapshot.Bullets?.Length ?? 0} bullets.");
		}

		private static void SpawnShips(GameplaySceneState state)
		{
			if (state.Ships == null)
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
