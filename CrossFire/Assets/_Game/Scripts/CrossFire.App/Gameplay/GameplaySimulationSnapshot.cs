using System;

namespace CrossFire.App
{
	/// <summary>
	/// Full serializable snapshot of a gameplay simulation at a point in time.
	/// Contains every piece of runtime state needed to reconstruct the world exactly:
	/// all ships with their physics, health and weapon state, all bullets in flight,
	/// and the stable-ID counter so future spawns do not collide with restored IDs.
	/// </summary>
	[Serializable]
	public class GameplaySimulationSnapshot
	{
		/// <summary>Value of <c>StableIdProvider._nextId</c> at save time.</summary>
		public int NextStableId;
		public ShipSaveData[] Ships;
		public BulletSaveData[] Bullets;
	}

	[Serializable]
	public struct ShipSaveData
	{
		// ── Identity ──────────────────────────────────────────────────────────
		public int StableId;
		public int ShipType;     // CrossFire.Ships.ShipType cast to int
		public byte Team;

		// ── Pose ──────────────────────────────────────────────────────────────
		public float PositionX;
		public float PositionY;
		public float ThetaRad;

		// ── Physics ───────────────────────────────────────────────────────────
		public float VelocityX;
		public float VelocityY;
		public float AngularVelocity;

		// ── State ─────────────────────────────────────────────────────────────
		public short Health;
		public float WeaponCooldown;
	}

	[Serializable]
	public struct BulletSaveData
	{
		// ── Identity ──────────────────────────────────────────────────────────
		public int BulletType;   // CrossFire.Combat.BulletType cast to int
		public byte Team;
		/// <summary>
		/// StableId of the ship that fired this bullet, or -1 if unresolvable.
		/// Resolved back to an Entity reference during load.
		/// </summary>
		public int OwnerStableId;

		// ── Pose ──────────────────────────────────────────────────────────────
		public float PositionX;
		public float PositionY;
		public float ThetaRad;

		// ── Physics ───────────────────────────────────────────────────────────
		public float VelocityX;
		public float VelocityY;

		// ── State ─────────────────────────────────────────────────────────────
		public float LifetimeRemaining;
		public short BulletDamage;
	}
}
