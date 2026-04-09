using NUnit.Framework;
using Unity.Entities;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Tests for CollisionEventCleanupSystem.
	/// The system clears the singleton CollisionEvent buffer at the end of each frame so
	/// events do not persist into the next frame. It is the safety net for frames where
	/// CollisionDetectionSystem does not run.
	/// </summary>
	public class CollisionEventCleanupSystemTests : PhysicsTestBase
	{
		private Entity _bufferEntity;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			RegisterSystem<CollisionEventCleanupSystem>();
			_bufferEntity = PhysicsEntityFactory.CreateCollisionSingletons(_entityManager, cellSize: 10f);
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		private DynamicBuffer<CollisionEvent> GetCollisionEvents()
		{
			return _entityManager.GetBuffer<CollisionEvent>(_bufferEntity);
		}

		/// <summary>
		/// Writes <paramref name="count"/> dummy events into the buffer so tests can
		/// verify the cleanup system clears them. Entity values are irrelevant here.
		/// </summary>
		private void PopulateBuffer(int count)
		{
			DynamicBuffer<CollisionEvent> buffer = GetCollisionEvents();
			for (int i = 0; i < count; i++)
			{
				buffer.Add(new CollisionEvent { FirstEntity = Entity.Null, SecondEntity = Entity.Null });
			}
		}

		// ─── Test 1 ───────────────────────────────────────────────────────────────
		// Core behaviour: any events present in the buffer must be cleared after update.

		[Test]
		public void OnUpdate_BufferHasEvents_AllEventsCleared()
		{
			PopulateBuffer(3);
			Assert.AreEqual(3, GetCollisionEvents().Length, "Pre-condition: buffer must start with 3 events");

			_world.Update();

			Assert.AreEqual(0, GetCollisionEvents().Length, "All events must be cleared after update");
		}

		// ─── Test 2 ───────────────────────────────────────────────────────────────
		// An already-empty buffer must stay empty without errors.

		[Test]
		public void OnUpdate_EmptyBuffer_RemainsEmpty()
		{
			Assert.AreEqual(0, GetCollisionEvents().Length, "Pre-condition: buffer must start empty");

			Assert.DoesNotThrow(() => _world.Update());

			Assert.AreEqual(0, GetCollisionEvents().Length, "Empty buffer must remain empty");
		}

		// ─── Test 3 ───────────────────────────────────────────────────────────────
		// Clearing must happen on every update — not just the first — so events
		// written between frames are always swept away.

		[Test]
		public void OnUpdate_EventsAddedBetweenUpdates_ClearedEachTime()
		{
			PopulateBuffer(2);
			_world.Update();
			Assert.AreEqual(0, GetCollisionEvents().Length, "First update must clear the buffer");

			PopulateBuffer(5);
			_world.Update();
			Assert.AreEqual(0, GetCollisionEvents().Length, "Second update must also clear the buffer");
		}

		// ─── Test 4 ───────────────────────────────────────────────────────────────
		// Without the CollisionEventBufferTag singleton the system must be skipped by
		// RequireForUpdate — no exceptions thrown.

		[Test]
		public void OnUpdate_WithoutSingleton_SystemSkippedWithoutError()
		{
			// Destroy the buffer entity so the singleton no longer exists.
			_entityManager.DestroyEntity(_bufferEntity);

			Assert.DoesNotThrow(() => _world.Update(),
				"System must be skipped gracefully when the singleton is absent");
		}
	}
}
