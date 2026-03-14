using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct WeaponFireSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BulletPrefabEntry>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRO<WorldPose> worldPose, RefRO<ControlIntent> controlIntent, RefRO<WeaponConfig> weaponConfig, RefRW<WeaponCooldown> weaponCooldown, Entity entity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<ControlIntent>, RefRO<WeaponConfig>, RefRW<WeaponCooldown>>()
						.WithEntityAccess())
			{
				if (controlIntent.ValueRO.Fire == 0)
				{
					continue;
				}

				if (weaponCooldown.ValueRO.TimeLeft > 0f)
				{
					continue;
				}

				weaponCooldown.ValueRW.TimeLeft = math.max(0.01f, weaponConfig.ValueRO.FireInterval);

				Entity prefabEntity = GetPrefabForType(weaponConfig.ValueRO.BulletType);
				if (prefabEntity == Entity.Null)
				{
					continue;
				}

				Entity bulletEntity = entityCommandBuffer.Instantiate(prefabEntity);

				byte teamId = entityManager.GetComponentData<TeamId>(entity).Value;

				entityCommandBuffer.SetComponent(bulletEntity, new Owner
				{
					Value = entity
				});

				entityCommandBuffer.SetComponent(bulletEntity, new TeamId
				{
					Value = teamId
				});

				if (SystemAPI.HasSingleton<TeamColor>())
				{
					DynamicBuffer<TeamColor> teamColors = SystemAPI.GetSingletonBuffer<TeamColor>(true);
					float4 color = CoreHelpers.GetTeamColor(teamColors, teamId);

					if (entityManager.HasComponent<NeedsColorRefresh>(prefabEntity))
					{
						entityCommandBuffer.SetComponent(bulletEntity, new NeedsColorRefresh
						{
							Value = color
						});
					}
					else
					{
						entityCommandBuffer.AddComponent(bulletEntity, new NeedsColorRefresh
						{
							Value = color
						});
					}
				}

				Pose2D shipWorldPose = worldPose.ValueRO.Value;
				float2 shipForward = PhysicsUtilities.Forward(shipWorldPose.ThetaRad);
				float2 bulletWorldPosition = shipWorldPose.Position + shipForward * weaponConfig.ValueRO.MuzzleOffset;

				entityCommandBuffer.SetComponent(bulletEntity, new WorldPose
				{
					Value = new Pose2D
					{
						Position = bulletWorldPosition,
						ThetaRad = shipWorldPose.ThetaRad
					}
				});

				if (entityManager.HasComponent<Velocity>(prefabEntity))
				{
					float2 shipVelocity = float2.zero;

					if (entityManager.HasComponent<Velocity>(entity))
					{
						shipVelocity = entityManager.GetComponentData<Velocity>(entity).Value;
					}

					entityCommandBuffer.SetComponent(bulletEntity, new Velocity
					{
						Value = shipForward * weaponConfig.ValueRO.BulletSpeed + shipVelocity
					});
				}

				if (entityManager.HasComponent<Lifetime>(prefabEntity))
				{
					entityCommandBuffer.SetComponent(bulletEntity, new Lifetime
					{
						TimeLeft = math.max(0.05f, weaponConfig.ValueRO.BulletLifetime)
					});
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
		}

		private Entity GetPrefabForType(BulletType bulletType)
		{
			DynamicBuffer<BulletPrefabEntry> entries = SystemAPI.GetSingletonBuffer<BulletPrefabEntry>(true);

			for (int index = 0; index < entries.Length; index++)
			{
				BulletPrefabEntry entry = entries[index];
				if (entry.Type == bulletType)
				{
					return entry.Prefab;
				}
			}

			return Entity.Null;
		}
	}
}