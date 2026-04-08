using NUnit.Framework;
using Unity.Entities;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Base class for all Core.Physics system tests.
	/// Creates a fresh isolated World per test and exposes helpers for registering systems.
	/// Each subclass registers only the system(s) under test via RegisterSystem&lt;T&gt;().
	/// </summary>
	public abstract class PhysicsTestBase
	{
		protected World _world;
		protected EntityManager _entityManager;
		protected SimulationSystemGroup _simulationSystemGroup;

		[SetUp]
		public virtual void SetUp()
		{
			_world = new World("PhysicsTestWorld");
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
		/// _world.Update() will execute it.
		/// </summary>
		protected SystemHandle RegisterSystem<T>() where T : unmanaged, ISystem
		{
			SystemHandle handle = _world.GetOrCreateSystem<T>();
			_simulationSystemGroup.AddSystemToUpdateList(handle);
			return handle;
		}
	}
}
