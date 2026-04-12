using CrossFire.App;
using CrossFire.Core;
using NUnit.Framework;
using Unity.Entities;

namespace CrossFire.Tests.EditMode
{
	/// <summary>
	/// Tests for SimulationPauseApi.
	/// Verifies that Pause, Resume, and IsPaused correctly manage the SimulationPaused singleton.
	/// </summary>
	public class SimulationPauseApiTests
	{
		private World _world;
		private EntityManager _entityManager;

		[SetUp]
		public void SetUp()
		{
			_world = new World("PauseApiTestWorld");
			_entityManager = _world.EntityManager;
		}

		[TearDown]
		public void TearDown()
		{
			_world.Dispose();
		}

		// ─── Pause ────────────────────────────────────────────────────────────────

		[Test]
		public void Pause_WhenNotPaused_CreatesSingletonComponent()
		{
			SimulationPauseApi.Pause(_entityManager);

			Assert.IsTrue(SimulationPauseApi.IsPaused(_entityManager));
		}

		[Test]
		public void Pause_WhenAlreadyPaused_DoesNotCreateDuplicateSingleton()
		{
			SimulationPauseApi.Pause(_entityManager);
			SimulationPauseApi.Pause(_entityManager);

			using EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SimulationPaused>());
			Assert.AreEqual(1, query.CalculateEntityCount(), "Pausing twice must not create a second SimulationPaused entity");
		}

		// ─── Resume ───────────────────────────────────────────────────────────────

		[Test]
		public void Resume_WhenPaused_RemovesSingletonComponent()
		{
			SimulationPauseApi.Pause(_entityManager);
			SimulationPauseApi.Resume(_entityManager);

			Assert.IsFalse(SimulationPauseApi.IsPaused(_entityManager));
		}

		[Test]
		public void Resume_WhenNotPaused_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => SimulationPauseApi.Resume(_entityManager));
		}

		// ─── IsPaused ─────────────────────────────────────────────────────────────

		[Test]
		public void IsPaused_WhenPaused_ReturnsTrue()
		{
			SimulationPauseApi.Pause(_entityManager);

			Assert.IsTrue(SimulationPauseApi.IsPaused(_entityManager));
		}

		[Test]
		public void IsPaused_WhenNotPaused_ReturnsFalse()
		{
			Assert.IsFalse(SimulationPauseApi.IsPaused(_entityManager));
		}

		[Test]
		public void IsPaused_AfterPauseThenResume_ReturnsFalse()
		{
			SimulationPauseApi.Pause(_entityManager);
			SimulationPauseApi.Resume(_entityManager);

			Assert.IsFalse(SimulationPauseApi.IsPaused(_entityManager));
		}
	}
}
