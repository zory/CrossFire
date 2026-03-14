using CrossFire.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	//[BurstCompile]
	public partial struct TargetRetargetTimerSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<TargetRetargetTimer>();
		}

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRW<TargetRetargetTimer> retargetTimer, RefRO<TargetingProfile> targetingProfile, Entity entity) in
					 SystemAPI.Query<RefRW<TargetRetargetTimer>, RefRO<TargetingProfile>>()
						.WithEntityAccess())
			{
				if (targetingProfile.ValueRO.Mode != TargetingMode.ThreatRetarget)
				{
					continue;
				}

				float interval = math.max(0.05f, targetingProfile.ValueRO.RetargetInterval);
				float newTimeLeft = retargetTimer.ValueRO.TimeLeft - deltaTime;

				if (newTimeLeft > 0f)
				{
					retargetTimer.ValueRW.TimeLeft = newTimeLeft;
					continue;
				}

				retargetTimer.ValueRW.TimeLeft = interval;

				if (!entityManager.HasComponent<NeedsTargetTag>(entity))
				{
					entityCommandBuffer.AddComponent<NeedsTargetTag>(entity);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
		}
	}
}