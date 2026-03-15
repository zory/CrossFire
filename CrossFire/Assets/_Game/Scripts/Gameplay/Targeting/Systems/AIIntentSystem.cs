using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AIIntentSystem : ISystem
	{
		private const float FireRange = 10f;
		private const float FireConeCos = 0.98f;

		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CurrentTarget>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			foreach ((RefRO<WorldPose> selfPose, RefRO<CurrentTarget> currentTarget, RefRW<ControlIntent> controlIntent) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<CurrentTarget>, RefRW<ControlIntent>>().
						WithNone<ControlledTag>())
			{
				Entity targetEntity = currentTarget.ValueRO.Value;

				if (targetEntity == Entity.Null || !entityManager.Exists(targetEntity) || !entityManager.HasComponent<WorldPose>(targetEntity))
				{
					controlIntent.ValueRW.Turn = 0f;
					controlIntent.ValueRW.Thrust = 0f;
					controlIntent.ValueRW.Fire = 0;
					continue;
				}

				Pose2D selfPoseValue = selfPose.ValueRO.Value;
				Pose2D targetPoseValue = entityManager.GetComponentData<WorldPose>(targetEntity).Value;

				float2 toTarget = targetPoseValue.Position - selfPoseValue.Position;
				float distanceSq = math.lengthsq(toTarget);

				float desiredTheta = math.atan2(-toTarget.x, toTarget.y);
				float deltaTheta = NormalizeAngle(desiredTheta - selfPoseValue.ThetaRad);

				float turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
				float thrust = 1f;

				float2 forward = new float2(-math.sin(selfPoseValue.ThetaRad), math.cos(selfPoseValue.ThetaRad));
				float2 directionToTarget = math.normalizesafe(toTarget);
				float facingDot = math.dot(forward, directionToTarget);

				byte fire = (byte)((distanceSq <= FireRange * FireRange && facingDot >= FireConeCos) ? 1 : 0);

				controlIntent.ValueRW.Turn = turn;
				controlIntent.ValueRW.Thrust = thrust;
				controlIntent.ValueRW.Fire = fire;
			}
		}

		[BurstCompile]
		private static float NormalizeAngle(float angle)
		{
			angle = math.fmod(angle + math.PI, 2f * math.PI);

			if (angle < 0f)
			{
				angle += 2f * math.PI;
			}

			return angle - math.PI;
		}
	}
}