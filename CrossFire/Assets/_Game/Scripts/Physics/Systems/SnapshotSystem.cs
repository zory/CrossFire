using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	/// <summary>
	/// Store previous frame state for interpolation / rollback / debug.
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
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
			foreach (var (pose, prevPose) in SystemAPI.Query<RefRO<WorldPose>, RefRW<PrevWorldPose>>())
			{
				//This just current position to previous position before new update
				prevPose.ValueRW.Value = pose.ValueRO.Value;
			}
		}
	}
}