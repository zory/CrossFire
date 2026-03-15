using CrossFire.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	public partial struct ManualTargetApplySystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<ManualTarget>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRO<ManualTarget> manualTarget, RefRW<CurrentTarget> currentTarget, RefRO<TargetingProfile> targetingProfile, RefRO<TeamId> selfTeam, Entity selfEntity) in
					 SystemAPI.Query<RefRO<ManualTarget>, RefRW<CurrentTarget>, RefRO<TargetingProfile>, RefRO<TeamId>>().
						WithEntityAccess())
			{
				if (targetingProfile.ValueRO.Mode != TargetingMode.Manual)
				{
					continue;
				}

				Entity targetEntity = manualTarget.ValueRO.Value;
				bool isValid = true;

				if (targetEntity == Entity.Null)
				{
					isValid = false;
				}
				else if (!entityManager.Exists(targetEntity))
				{
					isValid = false;
				}
				else if (targetEntity == selfEntity)
				{
					isValid = false;
				}
				else if (!entityManager.HasComponent<TargetableTag>(targetEntity))
				{
					isValid = false;
				}
				else if (!entityManager.HasComponent<TeamId>(targetEntity))
				{
					isValid = false;
				}
				else
				{
					byte targetTeamId = entityManager.GetComponentData<TeamId>(targetEntity).Value;
					if (targetTeamId == selfTeam.ValueRO.Value)
					{
						isValid = false;
					}
				}

				if (!isValid)
				{
					currentTarget.ValueRW.Value = Entity.Null;
					continue;
				}

				currentTarget.ValueRW.Value = targetEntity;

				if (entityManager.HasComponent<NeedsTargetTag>(selfEntity))
				{
					entityCommandBuffer.RemoveComponent<NeedsTargetTag>(selfEntity);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
		}
	}
}