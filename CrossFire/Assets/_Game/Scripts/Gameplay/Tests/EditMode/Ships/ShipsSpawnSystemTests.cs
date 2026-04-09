using CrossFire.Core;
using Core.Physics;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships.Tests.EditMode
{
	/// <summary>
	/// Tests for <see cref="ShipsSpawnSystem"/>.
	/// The system reads <see cref="SpawnShipsCommand"/> entries from the singleton command buffer,
	/// instantiates the matching prefab, and applies pose, team, and id to the new entity.
	/// </summary>
	public class ShipsSpawnSystemTests : ShipsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<ShipsSpawnSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// A valid command with a registered prefab must result in one new ship entity.

		[Test]
		public void OnUpdate_ValidCommand_SpawnsOneShip()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 1, Type = ShipType.Fighter, Team = 0, Pose = default });

			int entityCountBefore = CountShips();
			_world.Update();

			// Spawned entity count increased by 1; prefab entity does not count as a ship
			Assert.AreEqual(entityCountBefore + 1, CountShips(),
				"One ship should have been spawned");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// The spawned entity must have the WorldPose from the command.

		[Test]
		public void OnUpdate_ValidCommand_SpawnedShipHasCorrectWorldPose()
		{
			Pose2D spawnPose   = new Pose2D { Position = new float2(3f, 7f), ThetaRad = 1.2f };
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 0, Type = ShipType.Fighter, Team = 0, Pose = spawnPose });

			_world.Update();

			Entity spawned = GetFirstShipExcluding(prefab);
			Pose2D resultPose = _entityManager.GetComponentData<WorldPose>(spawned).Value;

			Assert.AreEqual(spawnPose.Position.x, resultPose.Position.x, 1e-5f, "Position.x");
			Assert.AreEqual(spawnPose.Position.y, resultPose.Position.y, 1e-5f, "Position.y");
			Assert.AreEqual(spawnPose.ThetaRad,   resultPose.ThetaRad,   1e-5f, "ThetaRad");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// The spawned entity must carry the TeamId from the command.

		[Test]
		public void OnUpdate_ValidCommand_SpawnedShipHasCorrectTeamId()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 0, Type = ShipType.Fighter, Team = 2, Pose = default });

			_world.Update();

			Entity spawned = GetFirstShipExcluding(prefab);
			byte teamId = _entityManager.GetComponentData<TeamId>(spawned).Value;

			Assert.AreEqual(2, teamId, "TeamId must match the spawn command");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// The spawned entity must carry the StableId from the command.

		[Test]
		public void OnUpdate_ValidCommand_SpawnedShipHasCorrectStableId()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 42, Type = ShipType.Fighter, Team = 0, Pose = default });

			_world.Update();

			Entity spawned = GetFirstShipExcluding(prefab);
			int stableId = _entityManager.GetComponentData<StableId>(spawned).Value;

			Assert.AreEqual(42, stableId, "StableId must match the command Id");
		}

		// ─── Test 5 ───────────────────────────────────────────────────────────────
		// A command for an unregistered ShipType must be silently skipped — no crash,
		// no entity spawned.

		[Test]
		public void OnUpdate_UnknownShipType_CommandSkipped_NoShipSpawned()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			// Bomber has no registered prefab in this test.
			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 0, Type = ShipType.Bomber, Team = 0, Pose = default });

			int countBefore = CountShips();
			_world.Update();

			Assert.AreEqual(countBefore, CountShips(),
				"Unknown ship type must not spawn any entity");
		}

		// ─── Test 6 ───────────────────────────────────────────────────────────────
		// After processing, the command buffer must be cleared so commands are not
		// re-processed on the next update.

		[Test]
		public void OnUpdate_AfterProcessing_CommandBufferIsCleared()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer).Add(
				new SpawnShipsCommand { Id = 0, Type = ShipType.Fighter, Team = 0, Pose = default });

			_world.Update();

			DynamicBuffer<SpawnShipsCommand> buffer =
				_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer);
			Assert.AreEqual(0, buffer.Length, "Command buffer must be empty after processing");
		}

		// ─── Test 7 ───────────────────────────────────────────────────────────────
		// Multiple commands in a single update must each spawn an independent ship.

		[Test]
		public void OnUpdate_MultipleCommands_SpawnsOneShipPerCommand()
		{
			Entity prefab      = ShipsEntityFactory.CreateShipPrefab(_entityManager, ShipType.Fighter);
			Entity commandBuffer = ShipsEntityFactory.CreateSpawnCommandBuffer(_entityManager);
			ShipsEntityFactory.CreateShipPrefabRegistry(_entityManager, ShipType.Fighter, prefab);

			DynamicBuffer<SpawnShipsCommand> buffer =
				_entityManager.GetBuffer<SpawnShipsCommand>(commandBuffer);
			buffer.Add(new SpawnShipsCommand { Id = 0, Type = ShipType.Fighter, Team = 0, Pose = default });
			buffer.Add(new SpawnShipsCommand { Id = 1, Type = ShipType.Fighter, Team = 1, Pose = default });
			buffer.Add(new SpawnShipsCommand { Id = 2, Type = ShipType.Fighter, Team = 0, Pose = default });

			int countBefore = CountShips();
			_world.Update();

			Assert.AreEqual(countBefore + 3, CountShips(),
				"Three commands must spawn three ships");
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		/// <summary>Counts all entities that have a WorldPose and a StableId (i.e. ships).</summary>
		private int CountShips()
		{
			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<StableId>());
			return query.CalculateEntityCount();
		}

		/// <summary>
		/// Returns the first ship entity that is not <paramref name="exclude"/>.
		/// Useful when the prefab entity shares the same archetype as spawned ships.
		/// </summary>
		private Entity GetFirstShipExcluding(Entity exclude)
		{
			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<StableId>());
			using Unity.Collections.NativeArray<Entity> entities =
				query.ToEntityArray(Unity.Collections.Allocator.Temp);

			for (int i = 0; i < entities.Length; i++)
			{
				if (entities[i] != exclude)
				{
					return entities[i];
				}
			}

			return Entity.Null;
		}
	}
}
