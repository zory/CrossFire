using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Combat
{
	/// <summary>
	/// Reduces cooldowns for weapons
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(SnapshotSystem))]
	[BurstCompile]
	public partial struct WeaponCooldownSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (RefRW<WeaponCooldown> cooldown in SystemAPI.Query<RefRW<WeaponCooldown>>())
			{
				float timer = cooldown.ValueRO.TimeLeft - deltaTime;
				cooldown.ValueRW.TimeLeft = (timer > 0f) ? timer : 0f;
			}
		}
	}
}