using CrossFire.Ships;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(ShipMovementSystem))]
	[BurstCompile]
	public partial struct CollisionSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
		}
	}
}