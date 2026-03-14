using CrossFire.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	//[BurstCompile]
	public partial struct TargetValidationSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CurrentTarget>();
		}

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRW<CurrentTarget> currentTarget, RefRO<TargetingProfile> targetingProfile, RefRO<TeamId> selfTeam, Entity selfEntity) in
					 SystemAPI.Query<RefRW<CurrentTarget>, RefRO<TargetingProfile>, RefRO<TeamId>>().
					 WithEntityAccess())
			{
				Entity targetEntity = currentTarget.ValueRO.Value;
				bool isInvalid = false;

				if (targetEntity == Entity.Null)
				{
					isInvalid = true;
				}
				else if (!entityManager.Exists(targetEntity))
				{
					isInvalid = true;
				}
				else if (!entityManager.HasComponent<TargetableTag>(targetEntity))
				{
					isInvalid = true;
				}
				else if (!entityManager.HasComponent<TeamId>(targetEntity))
				{
					isInvalid = true;
				}
				else
				{
					byte targetTeamId = entityManager.GetComponentData<TeamId>(targetEntity).Value;
					if (targetTeamId == selfTeam.ValueRO.Value)
					{
						isInvalid = true;
					}
				}

				if (!isInvalid)
				{
					continue;
				}

				currentTarget.ValueRW.Value = Entity.Null;

				if (targetingProfile.ValueRO.Mode == TargetingMode.Manual)
				{
					continue;
				}

				if (!entityManager.HasComponent<NeedsTargetTag>(selfEntity))
				{
					entityCommandBuffer.AddComponent<NeedsTargetTag>(selfEntity);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
		}
	}
}