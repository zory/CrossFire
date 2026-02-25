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
		// Include ShipTag in the query so only entities that also have ShipTag are iterated.
		foreach (var (tag, pos, prev) in SystemAPI.Query< RefRO <ShipTag>, RefRO<Pos>, RefRW<PrevPos>>())
		{
			//This just applies ship current position to previous position before new update
			prev.ValueRW.Value = pos.ValueRO.Value;
		}
	}
}