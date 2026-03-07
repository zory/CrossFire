using CrossFire.Lookup;
using CrossFire.Ships;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(ShipMovementSystem))]
	[BurstCompile]
	public partial struct CollisionSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			var em = state.EntityManager;

			// Filter singleton
			{
				Entity e = em.CreateEntity();
				em.AddComponentData(e, new CollisionGridSettings { CellSize = 4f });
			}
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
		}
	}
}