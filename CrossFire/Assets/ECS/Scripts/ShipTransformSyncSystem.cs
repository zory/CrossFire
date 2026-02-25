using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShipSimSystem))]
[BurstCompile]
public partial struct ShipTransformSyncSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<ShipTag>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach (var (tag, pos, ang, localTransport) in SystemAPI.Query<RefRO<ShipTag>, RefRO<Pos>, RefRO<Angle>, RefRW<LocalTransform>>())
		{
			float2 position = pos.ValueRO.Value;
			localTransport.ValueRW.Position = new float3(position.x, position.y, 0f);

			float theta = ang.ValueRO.Value;
			localTransport.ValueRW.Rotation = quaternion.RotateZ(theta);
		}
	}
}