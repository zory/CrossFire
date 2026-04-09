using NUnit.Framework;
using Unity.Entities;

namespace CrossFire.Ships.Tests.EditMode
{
	/// <summary>
	/// Tests for <see cref="ShipsSpawnCommandBufferSystem"/>.
	/// The system is a one-shot bootstrap: it creates one singleton entity that holds the
	/// <see cref="DynamicBuffer{T}"/> of <see cref="SpawnShipsCommand"/> and then disables
	/// itself so <c>OnUpdate</c> is never called again.
	/// </summary>
	public class ShipsSpawnCommandBufferSystemTests : ShipsTestBase
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<ShipsSpawnCommandBufferSystem>();
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// The system must create exactly one entity tagged with SpawnShipsCommandBufferTag.

		[Test]
		public void OnCreate_CreatesExactlyOneCommandBufferEntity()
		{
			_world.Update();

			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<SpawnShipsCommandBufferTag>());

			Assert.AreEqual(1, query.CalculateEntityCount(),
				"Expected exactly one SpawnShipsCommandBufferTag entity");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// The command buffer must start empty — no leftover commands at startup.

		[Test]
		public void OnCreate_CommandBufferStartsEmpty()
		{
			_world.Update();

			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<SpawnShipsCommandBufferTag>());
			Entity bufferEntity = query.GetSingletonEntity();

			DynamicBuffer<SpawnShipsCommand> buffer =
				_entityManager.GetBuffer<SpawnShipsCommand>(bufferEntity);

			Assert.AreEqual(0, buffer.Length, "Command buffer must be empty after bootstrap");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// After the first update the system disables itself — a second update must not
		// create a duplicate entity.

		[Test]
		public void AfterFirstUpdate_SystemDisablesItself_NoDuplicateOnSecondUpdate()
		{
			_world.Update();
			_world.Update();

			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<SpawnShipsCommandBufferTag>());

			Assert.AreEqual(1, query.CalculateEntityCount(),
				"A second Update must not create a second command buffer entity");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// The created entity must carry a DynamicBuffer<SpawnShipsCommand> so callers
		// can enqueue commands without a separate AddBuffer call.

		[Test]
		public void OnCreate_CreatedEntityHasSpawnShipsCommandBuffer()
		{
			_world.Update();

			using EntityQuery query = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<SpawnShipsCommandBufferTag>());
			Entity bufferEntity = query.GetSingletonEntity();

			Assert.IsTrue(
				_entityManager.HasBuffer<SpawnShipsCommand>(bufferEntity),
				"Buffer entity must carry DynamicBuffer<SpawnShipsCommand>");
		}
	}
}
