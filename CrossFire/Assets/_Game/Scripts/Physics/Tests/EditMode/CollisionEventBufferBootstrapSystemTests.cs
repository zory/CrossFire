using NUnit.Framework;
using Unity.Entities;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for CollisionEventBufferBootstrapSystem.
	/// The system runs entirely in OnCreate: it creates the singleton
	/// CollisionEventBufferTag entity with an empty CollisionEvent buffer if one does
	/// not already exist, then disables itself so OnUpdate is never called.
	/// </summary>
	public class CollisionEventBufferBootstrapSystemTests : PhysicsTestBase
	{
		// ─── Helpers ──────────────────────────────────────────────────────────────

		private int CountEntitiesWithTag()
		{
			EntityQuery query = _entityManager.CreateEntityQuery(typeof(CollisionEventBufferTag));
			int count = query.CalculateEntityCount();
			query.Dispose();
			return count;
		}

		private Entity GetBufferEntity()
		{
			EntityQuery query = _entityManager.CreateEntityQuery(typeof(CollisionEventBufferTag));
			Entity entity = query.GetSingletonEntity();
			query.Dispose();
			return entity;
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// When no singleton exists the system must create exactly one entity carrying
		// CollisionEventBufferTag.

		[Test]
		public void OnCreate_NoPriorSingleton_CreatesExactlyOneBufferEntity()
		{
			RegisterSystem<CollisionEventBufferBootstrapSystem>();
			_world.Update();

			Assert.AreEqual(1, CountEntitiesWithTag(),
				"Exactly one CollisionEventBufferTag entity must be created");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// The created entity must have a DynamicBuffer<CollisionEvent> that starts empty.

		[Test]
		public void OnCreate_NoPriorSingleton_BufferStartsEmpty()
		{
			RegisterSystem<CollisionEventBufferBootstrapSystem>();
			_world.Update();

			Entity bufferEntity = GetBufferEntity();
			Assert.IsTrue(_entityManager.HasBuffer<CollisionEvent>(bufferEntity),
				"Created entity must have a DynamicBuffer<CollisionEvent>");
			Assert.AreEqual(0, _entityManager.GetBuffer<CollisionEvent>(bufferEntity).Length,
				"Buffer must start empty");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// When the singleton already exists before the system is registered, the system
		// must leave it untouched and not create a second entity.

		[Test]
		public void OnCreate_SingletonAlreadyExists_DoesNotCreateDuplicate()
		{
			// Create the singleton manually before the bootstrap system is registered.
			Entity existingEntity = _entityManager.CreateEntity();
			_entityManager.AddComponent<CollisionEventBufferTag>(existingEntity);
			_entityManager.AddBuffer<CollisionEvent>(existingEntity);

			RegisterSystem<CollisionEventBufferBootstrapSystem>();
			_world.Update();

			Assert.AreEqual(1, CountEntitiesWithTag(),
				"No duplicate must be created when the singleton already exists");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// The system must disable itself after OnCreate so that repeated world updates
		// do not create additional entities.

		[Test]
		public void OnCreate_AfterSetup_SystemIsDisabledAndDoesNotDuplicate()
		{
			SystemHandle handle = RegisterSystem<CollisionEventBufferBootstrapSystem>();
			_world.Update();

			Assert.AreEqual(1, CountEntitiesWithTag(), "One entity after first update");

			// Run several more updates — a re-enabled system could create duplicates.
			_world.Update();
			_world.Update();

			Assert.AreEqual(1, CountEntitiesWithTag(),
				"Entity count must remain 1 across multiple updates");
		}
	}
}
