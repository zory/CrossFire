using CrossFire.Ships;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(ShipsSpawnSystem))]
	[BurstCompile]
	public partial struct SnapshotSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// Include ShipTag in the query so only entities that also have ShipTag are iterated.
			foreach (var (pose, prevPose) in SystemAPI.Query<RefRO<WorldPose>, RefRW<PrevWorldPose>>())
			{
				//This just applies ship current position to previous position before new update
				prevPose.ValueRW.Value = pose.ValueRO.Value;
			}
		}
	}
}