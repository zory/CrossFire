using CrossFire.Core;
using Core.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Combat
{
	/// <summary>
	/// Fires bullets when ControlIntent.Fire is present nad cooldown allows it
	/// </summary>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct WeaponFireSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (worldPoseRO, controlIntentRO, weaponConfigRO, weaponCooldownRW, entity) in
					SystemAPI.Query<RefRO<WorldPose>, RefRO<ControlIntent>, RefRO<WeaponConfig>, RefRW<WeaponCooldown>>().
						WithEntityAccess())
			{
				//No fire intent - skip
				if (controlIntentRO.ValueRO.Fire == 0)
				{
					continue;
				}

				//Weapon is not ready - skip
				if (weaponCooldownRW.ValueRO.TimeLeft > 0f)
				{
					continue;
				}

				// Reset cooldown
				weaponCooldownRW.ValueRW.TimeLeft = weaponConfigRO.ValueRO.FireInterval;

				//Select bullet prefab
				Entity prefabEntity = GetPrefabForType(ref state, weaponConfigRO.ValueRO.BulletType);
				if (prefabEntity == Entity.Null)
				{
					continue;
				}

				//instantiate
				Entity bullet = entityCommandBuffer.Instantiate(prefabEntity);

				//set owner/team
				byte teamId = entityManager.GetComponentData<TeamId>(entity).Value;
				entityCommandBuffer.SetComponent<Owner>(bullet, new Owner { Value = entity });
				entityCommandBuffer.SetComponent<TeamId>(bullet, new TeamId() { Value = teamId });

				//Request color change when possible
				if (SystemAPI.HasSingleton<TeamColor>())
				{
					Entity teamColorEntity = SystemAPI.GetSingletonEntity<TeamColor>();
					DynamicBuffer<TeamColor> teamColors = SystemAPI.GetBuffer<TeamColor>(teamColorEntity);
					float4 color = CoreHelpers.GetTeamColor(teamColors, teamId);
					if (entityManager.HasComponent<NeedsColorRefresh>(prefabEntity))
					{
						entityCommandBuffer.SetComponent(bullet, new NeedsColorRefresh
						{
							Value = color
						});
					}
					else
					{
						entityCommandBuffer.AddComponent(bullet, new NeedsColorRefresh
						{
							Value = color
						});
					}
				}

				//set world pose
				Pose2D shipWorldPose = worldPoseRO.ValueRO.Value;
				float2 shipForward = PhysicsUtilities.Forward(shipWorldPose.ThetaRad);
				float muzzleOffset = weaponConfigRO.ValueRO.MuzzleOffset;
				float2 bulletWorldPosition = shipWorldPose.Position + shipForward * muzzleOffset;
				WorldPose worldPose = new WorldPose { Value = new Pose2D() { Position = bulletWorldPosition, ThetaRad = shipWorldPose.ThetaRad } };
				entityCommandBuffer.SetComponent<WorldPose>(bullet, worldPose);

				// Velocity
				if (entityManager.HasComponent<Velocity>(prefabEntity))
				{
					float2 shipVelocity = float2.zero;
					//if ship has velocity
					if (entityManager.HasComponent<Velocity>(entity))
					{
						shipVelocity = entityManager.GetComponentData<Velocity>(entity).Value;
					}

					float2 bulletVelocity = shipForward * weaponConfigRO.ValueRO.BulletSpeed;
					Velocity velocity = new Velocity()
					{
						Value = bulletVelocity + shipVelocity
					};
					entityCommandBuffer.SetComponent(bullet, velocity);
				}

				// Lifetime
				if (entityManager.HasComponent<Lifetime>(prefabEntity))
				{
					Lifetime lifeTime = new Lifetime()
					{
						TimeLeft = weaponConfigRO.ValueRO.BulletLifetime
					};
					entityCommandBuffer.SetComponent(bullet, lifeTime);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
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