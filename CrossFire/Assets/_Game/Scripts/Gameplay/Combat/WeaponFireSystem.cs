using CrossFire.Core;
using CrossFire.Physics;
using CrossFire.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Combat
{
	/// <summary>
	/// Fires bullets when ControlIntent.Fire is present nad cooldown allows it
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(WeaponCooldownSystem))]
	[BurstCompile]
	public partial struct WeaponFireSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (poseRO, intentRO, weaponRO, cdRW, entity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<ControlIntent>, RefRO<WeaponConfig>, RefRW<WeaponCooldown>>()
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

				float2 forward = new float2(-math.sin(ship.ThetaRad), math.cos(ship.ThetaRad));
				float muzzleOffset = weaponRO.ValueRO.MuzzleOffset;
				float2 bulletPos2 = ship.Position + forward * muzzleOffset;

				float2 bulletVel = forward * weaponRO.ValueRO.BulletSpeed;

				Entity prefabEntity = GetPrefabForType(ref state, weaponRO.ValueRO.BulletType);
				if (prefabEntity == Entity.Null)
				{
					// unknown type: skip
					continue;
				}
				// Check what components bullet prefab already has to choose Add vs Set.
				bool prefabHasVelocity = em.HasComponent<Velocity>(prefabEntity);
				bool prefabHasLifetime = em.HasComponent<Lifetime>(prefabEntity);

				var b = ecb.Instantiate(prefabEntity);

				// Bullet identity
				ecb.SetComponent(b, new Owner { Value = entity });
				ecb.SetComponent(b, new TeamId() { Value = em.GetComponentData<TeamId>(entity).Value });

				float4 color;
				if (em.GetComponentData<TeamId>(entity).Value == 0)
				{
					color = new float4(0, 0, 255, 255);
				}
				else
				{
					color = new float4(255, 0, 0, 255);
				}
				ecb.SetComponent(b, new URPMaterialPropertyBaseColor { Value = color });

				// Transform
				WorldPose worldPose = new WorldPose { Value = new Pose2D() { Position = bulletPos2, ThetaRad = ship.ThetaRad } };

				ecb.SetComponent(b, worldPose);

				// Velocity
				float2 shipVel = float2.zero;
				if (em.HasComponent<Velocity>(entity))
				{
					shipVel = em.GetComponentData<Velocity>(entity).Value;
				}

				var vel = new Velocity { Value = bulletVel + shipVel };
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

		private Entity GetPrefabForType(ref SystemState state, BulletType bulletType)
		{
			DynamicBuffer<BulletPrefabEntry> entries = SystemAPI.GetSingletonBuffer<BulletPrefabEntry>(true);

			for (int index = 0; index < entries.Length; index++)
			{
				BulletPrefabEntry entry = entries[index];
				if (entry.Type == bulletType)
					return entry.Prefab;
			}

			return Entity.Null;
		}
	}
}