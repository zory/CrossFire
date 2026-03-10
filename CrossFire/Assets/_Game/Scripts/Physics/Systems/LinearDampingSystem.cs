using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Physics
{
	[BurstCompile]
	public partial struct LinearDampingSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (var (velRW, dampingRO) in
					 SystemAPI.Query<RefRW<Velocity>, RefRO<LinearDamping>>())
			{
				float d = math.max(0f, dampingRO.ValueRO.Value);
				velRW.ValueRW.Value *= math.exp(-d * dt);
			}
		}
	}
}