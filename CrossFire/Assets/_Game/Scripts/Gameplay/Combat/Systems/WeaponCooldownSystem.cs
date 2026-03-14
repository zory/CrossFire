using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct WeaponCooldownSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (RefRW<WeaponCooldown> weaponCooldown in SystemAPI.Query<RefRW<WeaponCooldown>>())
			{
				float timer = weaponCooldown.ValueRO.TimeLeft - deltaTime;
				weaponCooldown.ValueRW.TimeLeft = (timer > 0f) ? timer : 0f;
			}
		}
	}
}