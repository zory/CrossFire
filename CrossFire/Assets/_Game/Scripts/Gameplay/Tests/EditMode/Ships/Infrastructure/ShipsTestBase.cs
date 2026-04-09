using NUnit.Framework;
using Unity.Core;
using Unity.Entities;

namespace CrossFire.Ships.Tests.EditMode
{
	/// <summary>
	/// Base class for all CrossFire.Ships system tests.
	/// Creates a fresh isolated World per test and exposes helpers for registering systems.
	/// Each subclass registers only the system(s) under test via <see cref="RegisterSystem{T}"/>.
	/// </summary>
	public abstract class ShipsTestBase
	{
		protected World _world;
		protected EntityManager _entityManager;
		protected SimulationSystemGroup _simulationSystemGroup;

		[SetUp]
		public virtual void SetUp()
		{
			_world = new World("ShipsTestWorld");
			_simulationSystemGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			_entityManager = _world.EntityManager;
		}

		[TearDown]
		public virtual void TearDown()
		{
			_world.Dispose();
		}

		/// <summary>
		/// Creates the system and adds it to the SimulationSystemGroup so that
		/// <c>_world.Update()</c> will execute it.
		/// </summary>
		protected SystemHandle RegisterSystem<T>() where T : unmanaged, ISystem
		{
			SystemHandle handle = _world.GetOrCreateSystem<T>();
			_simulationSystemGroup.AddSystemToUpdateList(handle);
			return handle;
		}

		/// <summary>
		/// Sets the world clock so that <c>SystemAPI.Time.DeltaTime</c> returns the given value.
		/// Required by any test that depends on time-stepped math (e.g. thrust integration).
		/// Call this before <c>_world.Update()</c>.
		/// </summary>
		protected void SetDeltaTime(float deltaTime)
		{
			_world.SetTime(new TimeData(elapsedTime: 0.0, deltaTime: deltaTime));
		}
	}
}
