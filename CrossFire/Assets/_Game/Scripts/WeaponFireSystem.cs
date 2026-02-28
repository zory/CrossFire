using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using CrossFire.Ships;

namespace CrossFire.Bullets
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(WeaponCooldownSystem))]
	[BurstCompile]
	public partial struct WeaponFireSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BulletPrefabReference>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;
			var bulletPrefab = SystemAPI.GetSingleton<BulletPrefabReference>().Prefab;

			// Check what components bullet prefab already has to choose Add vs Set.
			bool prefabHasVelocity = em.HasComponent<Velocity>(bulletPrefab);
			bool prefabHasLifetime = em.HasComponent<Lifetime>(bulletPrefab);

			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (poseRO, intentRO, weaponRO, cdRW, entity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<ShipIntent>, RefRO<WeaponConfig>, RefRW<WeaponCooldown>>()
							  .WithEntityAccess())
			{
				if (intentRO.ValueRO.Fire == 0)
					continue;

				if (cdRW.ValueRO.TimeLeft > 0f)
					continue;

				// Reset cooldown
				float interval = math.max(0.01f, weaponRO.ValueRO.FireInterval);
				cdRW.ValueRW.TimeLeft = interval;

				Pose2D ship = poseRO.ValueRO.Value;

				float2 forward = new float2(-math.sin(ship.Theta), math.cos(ship.Theta));
				float muzzleOffset = weaponRO.ValueRO.MuzzleOffset;
				float2 bulletPos2 = ship.Position + forward * muzzleOffset;

				float2 bulletVel = forward * weaponRO.ValueRO.BulletSpeed;

				var b = ecb.Instantiate(bulletPrefab);

				// Bullet identity
				ecb.AddComponent(b, new BulletTag());
				ecb.AddComponent(b, new BulletOwner { Value = entity });
				ecb.AddComponent(b, new TeamId() { Value = em.GetComponentData<TeamId>(entity).Value });

				// Transform
				WorldPose worldPose = new WorldPose { Value = new Pose2D() { Position = bulletPos2, Theta = ship.Theta } };

				ecb.SetComponent(b, worldPose);

				// Velocity
				var vel = new Velocity { Value = bulletVel };
				if (prefabHasVelocity) ecb.SetComponent(b, vel);
				else ecb.AddComponent(b, vel);

				// Lifetime
				var life = new Lifetime { TimeLeft = math.max(0.05f, weaponRO.ValueRO.BulletLifetime) };
				if (prefabHasLifetime) ecb.SetComponent(b, life);
				else ecb.AddComponent(b, life);
			}

			ecb.Playback(em);
			ecb.Dispose();
		}
	}
}