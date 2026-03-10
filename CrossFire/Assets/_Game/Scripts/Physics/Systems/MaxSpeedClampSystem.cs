using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
	[BurstCompile]
	public partial struct MaxSpeedClampSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (velRW, maxSpeedRO) in
					 SystemAPI.Query<RefRW<Velocity>, RefRO<MaxVelocity>>())
			{
				float2 v = velRW.ValueRO.Value;
				float maxV = math.max(0f, maxSpeedRO.ValueRO.Value);
				float vSq = math.lengthsq(v);

				if (vSq > maxV * maxV && vSq > 1e-8f)
					v *= maxV * math.rsqrt(vSq);

				velRW.ValueRW.Value = v;
			}
		}
	}
}