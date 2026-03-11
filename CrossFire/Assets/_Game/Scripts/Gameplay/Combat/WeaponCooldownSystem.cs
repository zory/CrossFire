using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Bullets
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[BurstCompile]
	public partial struct WeaponCooldownSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (RefRW<WeaponCooldown> cd in SystemAPI.Query<RefRW<WeaponCooldown>>())
			{
				float t = cd.ValueRO.TimeLeft - dt;
				cd.ValueRW.TimeLeft = (t > 0f) ? t : 0f;
			}
		}
	}
}