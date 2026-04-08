using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(PositionIntegrationSystem))]
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct MaxVelocityClampSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (velRW, maxSpeedRO) in SystemAPI.Query<RefRW<Velocity>, RefRO<MaxVelocity>>())
			{
				float2 velocity = velRW.ValueRO.Value;
				float maxVelocity = math.max(0f, maxSpeedRO.ValueRO.Value);
				float vSq = math.lengthsq(velocity);

				if (vSq > maxVelocity * maxVelocity && vSq > 1e-8f)
				{
					velocity *= maxVelocity * math.rsqrt(vSq);
				}

				velRW.ValueRW.Value = velocity;
			}
		}
	}
}