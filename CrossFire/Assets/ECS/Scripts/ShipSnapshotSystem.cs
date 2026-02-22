using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ShipSimSystem))]
[BurstCompile]
public partial struct ShipSnapshotSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<ShipTag>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach (var (pos, prev) in SystemAPI.Query<RefRO<ShipPos>, RefRW<ShipPrevPos>>())
		{
			prev.ValueRW.Value = pos.ValueRO.Value;
		}
	}
}