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
		foreach (var (pos, ang, xform) in SystemAPI.Query<RefRO<ShipPos>, RefRO<ShipAngle>, RefRW<LocalTransform>>())
		{
			float2 p = pos.ValueRO.Value;
			float theta = ang.ValueRO.Value;

			xform.ValueRW.Position = new float3(p.x, p.y, 0f);
			xform.ValueRW.Rotation = quaternion.RotateZ(theta);
		}
	}
}