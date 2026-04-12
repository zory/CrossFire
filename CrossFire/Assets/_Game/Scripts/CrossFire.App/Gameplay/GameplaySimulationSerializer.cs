using System.Collections.Generic;
using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Converts between the live ECS world and a <see cref="GameplaySimulationSnapshot"/>.
	///
	/// <para><b>Capture</b> — reads all ships and bullets from the ECS world and builds a
	/// snapshot.  Can be called while the simulation is paused.</para>
	///
	/// <para><b>Restore</b> — destroys every ship and bullet currently in the world, then
	/// re-creates them directly via <see cref="EntityManager"/> (not command buffers) so it
	/// works while the simulation is paused. Bullet <see cref="Owner"/> references are
	/// resolved by mapping owner stable-IDs back to the freshly spawned ship entities.</para>
	///
	/// <para>All ECS operations are delegated to <see cref="GameplaySimulationOperations"/>.</para>
	/// </summary>
	public static class GameplaySimulationSerializer
	{
		// ─── Public API ───────────────────────────────────────────────────────────

		public static GameplaySimulationSnapshot CaptureSnapshot(EntityManager em)
		{
			return new GameplaySimulationSnapshot
			{
				NextStableId = StableIdProvider.Peek(),
				Ships        = GameplaySimulationOperations.CaptureShips(em),
				Bullets      = GameplaySimulationOperations.CaptureBullets(em),
			};
		}

		public static void RestoreSnapshot(GameplaySimulationSnapshot snapshot, EntityManager em)
		{
			if (snapshot == null)
			{
				Debug.LogWarning("[GameplaySimulationSerializer] RestoreSnapshot called with null snapshot.");
				return;
			}

			GameplaySimulationOperations.DestroyAllShips(em);
			GameplaySimulationOperations.DestroyAllBullets(em);

			StableIdProvider.Restore(snapshot.NextStableId);

			Dictionary<int, Entity> stableIdToEntity = RestoreShips(em, snapshot.Ships);
			RestoreBullets(em, snapshot.Bullets, stableIdToEntity);
		}

		// ─── Restore helpers ──────────────────────────────────────────────────────

		private static Dictionary<int, Entity> RestoreShips(EntityManager em, ShipSaveData[] ships)
		{
			Dictionary<int, Entity> stableIdToEntity = new Dictionary<int, Entity>();

			if (ships == null)
			{
				return stableIdToEntity;
			}

			foreach (ShipSaveData data in ships)
			{
				Entity entity = GameplaySimulationOperations.SpawnShip(em, data);
				if (entity != Entity.Null)
				{
					stableIdToEntity[data.StableId] = entity;
				}
			}

			return stableIdToEntity;
		}

		private static void RestoreBullets(EntityManager em, BulletSaveData[] bullets, Dictionary<int, Entity> stableIdToEntity)
		{
			if (bullets == null)
			{
				return;
			}

			foreach (BulletSaveData data in bullets)
			{
				GameplaySimulationOperations.SpawnBullet(em, data, stableIdToEntity);
			}
		}
	}
}
