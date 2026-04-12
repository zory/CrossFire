using CrossFire.App;
using Core.Physics;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Tests.EditMode
{
	/// <summary>
	/// Behavioral tests for the AppSimulationPipeline pause mechanism.
	/// Uses PositionIntegrationSystem as an observable proxy: if the pipeline ran, a body
	/// with non-zero velocity will have a different position after world.Update().
	/// All other pipeline systems are safe to create in a test world — they either use
	/// RequireForUpdate guards or only act on entities that match specific queries.
	/// </summary>
	public class AppSimulationPipelinePauseTests
	{
		private World _world;
		private EntityManager _entityManager;

		[SetUp]
		public void SetUp()
		{
			_world = new World("PipelinePauseTestWorld");
			_entityManager = _world.EntityManager;

			SimulationSystemGroup simulationGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			AppSimulationPipeline pipeline = _world.GetOrCreateSystemManaged<AppSimulationPipeline>();
			simulationGroup.AddSystemToUpdateList(pipeline);
		}

		[TearDown]
		public void TearDown()
		{
			_world.Dispose();
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		/// <summary>
		/// Creates a dynamic body at the origin with the given velocity.
		/// WorldPose and Velocity are sufficient for PositionIntegrationSystem.
		/// </summary>
		private Entity CreateMovingBody(float2 velocity)
		{
			Entity entity = _entityManager.CreateEntity();
			_entityManager.AddComponentData(entity, new WorldPose
			{
				Value = new Pose2D { Position = float2.zero, ThetaRad = 0f }
			});
			_entityManager.AddComponentData(entity, new Velocity { Value = velocity });
			return entity;
		}

		private void SetDeltaTime(float deltaTime)
		{
			_world.SetTime(new TimeData(elapsedTime: 0.0, deltaTime: deltaTime));
		}

		// ─── Tests ────────────────────────────────────────────────────────────────

		[Test]
		public void OnUpdate_WhenPaused_DoesNotAdvanceSimulation()
		{
			Entity entity = CreateMovingBody(new float2(10f, 0f));
			SimulationPauseApi.Pause(_entityManager);
			SetDeltaTime(1.0f);

			_world.Update();

			float2 position = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			Assert.AreEqual(float2.zero, position, "Position must remain zero when simulation is paused");
		}

		[Test]
		public void OnUpdate_WhenNotPaused_AdvancesSimulation()
		{
			Entity entity = CreateMovingBody(new float2(10f, 0f));
			SetDeltaTime(1.0f);

			_world.Update();

			float2 position = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			Assert.AreNotEqual(float2.zero, position, "Position must advance when simulation is running");
		}

		[Test]
		public void OnUpdate_AfterPauseThenResume_AdvancesSimulation()
		{
			Entity entity = CreateMovingBody(new float2(10f, 0f));
			SetDeltaTime(1.0f);

			SimulationPauseApi.Pause(_entityManager);
			_world.Update(); // paused — position stays at zero

			SimulationPauseApi.Resume(_entityManager);
			_world.Update(); // resumed — position should advance

			float2 position = _entityManager.GetComponentData<WorldPose>(entity).Value.Position;
			Assert.AreNotEqual(float2.zero, position, "Position must advance after the simulation is resumed");
		}
	}
}
