using System;
using Core.Physics;
using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	/// <summary>
	/// Static helper that issues <see cref="SpawnShipsCommand"/> requests into the ECS
	/// command buffer. No MonoBehaviour or scene object required — call from anywhere.
	///
	/// Stable IDs are sourced from <see cref="StableIdProvider"/> so they are unique
	/// across all entity types (ships, bullets, etc.) within a session.
	/// </summary>
	/// <remarks>
	/// TODO: Add a <c>SpawnAll(ShipSpawnEntry[])</c> overload so scenes can declare their
	/// ship roster as a data array (in the Inspector or a ScriptableObject) instead of
	/// writing spawning code. <see cref="ShipSpawnEntry"/> already defines the per-ship
	/// data contract — wire it up when data-driven scene setup is needed.
	/// </remarks>
	public static class ShipSpawner
	{
		/// <summary>
		/// Issues a command to spawn one ship. The stable ID is assigned automatically
		/// from <see cref="StableIdProvider"/>.
		/// </summary>
		public static void Spawn(ShipType type, byte team, Pose2D pose)
		{
			PostCommand(new SpawnShipsCommand
			{
				Id = StableIdProvider.Next(),
				Type = type,
				Team = team,
				Pose = pose
			});
		}

		private static void PostCommand(SpawnShipsCommand command)
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
			Entity bufferEntity = query.GetSingletonEntity();
			DynamicBuffer<SpawnShipsCommand> buffer = entityManager.GetBuffer<SpawnShipsCommand>(bufferEntity);
			buffer.Add(command);
			query.Dispose();
		}
	}

	/// <summary>
	/// Data contract for a single ship entry in a spawn roster.
	/// Used by the future data-driven <c>SpawnAll</c> path on <see cref="ShipSpawner"/>.
	/// </summary>
	[Serializable]
	public struct ShipSpawnEntry
	{
		public ShipType Type;
		public byte Team;
		public Pose2D Pose;
	}
}
