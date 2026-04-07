using CrossFire.Core;
using CrossFire.Physics;
using CrossFire.Targeting;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships
{
	// Writes Turn and Thrust into ControlIntent based on MovementTarget.
	// Does NOT touch ControlIntent.Fire — that is owned by AIFireSystem.
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AIShipMovementIntentSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<MovementTarget>();
			state.RequireForUpdate<MovementTargetResolved>();
			state.RequireForUpdate<WorldPose>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach ((RefRO<WorldPose> selfPose,
					  RefRO<MovementTarget> movementTarget,
					  RefRO<MovementTargetResolved> movementTargetResolved,
					  RefRW<ControlIntent> controlIntent) in
					 SystemAPI.Query<
						 RefRO<WorldPose>,
						 RefRO<MovementTarget>,
						 RefRO<MovementTargetResolved>,
						 RefRW<ControlIntent>>()
					 .WithNone<ControlledTag>())
			{
				if (movementTargetResolved.ValueRO.HasTarget == 0 ||
					movementTarget.ValueRO.Mode == MovementTargetMode.None)
				{
					controlIntent.ValueRW.Turn = 0f;
					controlIntent.ValueRW.Thrust = 0f;
					continue;
				}

				Pose2D selfPoseValue = selfPose.ValueRO.Value;
				float2 targetPosition = movementTargetResolved.ValueRO.WorldPosition;

				switch (movementTarget.ValueRO.Mode)
				{
					case MovementTargetMode.FlyToPoint:
						{
							WriteFlyToIntent(
								selfPoseValue,
								targetPosition,
								movementTarget.ValueRO.ArrivalDistance,
								ref controlIntent.ValueRW);
							break;
						}

					case MovementTargetMode.ChaseAtRange:
					case MovementTargetMode.DefendClose:
						{
							WriteRangeIntent(
								selfPoseValue,
								targetPosition,
								movementTarget.ValueRO.PreferredDistance,
								movementTarget.ValueRO.DistanceTolerance,
								ref controlIntent.ValueRW);
							break;
						}

					default:
						{
							controlIntent.ValueRW.Turn = 0f;
							controlIntent.ValueRW.Thrust = 0f;
							break;
						}
				}
			}
		}

		[BurstCompile]
		private static void WriteFlyToIntent(
			Pose2D selfPose,
			float2 targetPosition,
			float arrivalDistance,
			ref ControlIntent controlIntent)
		{
			float2 toTarget = targetPosition - selfPose.Position;
			float distanceSq = math.lengthsq(toTarget);
			float arrivalDistanceSq = arrivalDistance * arrivalDistance;

			if (distanceSq <= arrivalDistanceSq)
			{
				controlIntent.Turn = 0f;
				controlIntent.Thrust = 0f;
				return;
			}

			float desiredTheta = math.atan2(-toTarget.x, toTarget.y);
			float deltaTheta = NormalizeAngle(desiredTheta - selfPose.ThetaRad);

			controlIntent.Turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
			controlIntent.Thrust = 1f;
		}

		[BurstCompile]
		private static void WriteRangeIntent(
			Pose2D selfPose,
			float2 targetPosition,
			float preferredDistance,
			float distanceTolerance,
			ref ControlIntent controlIntent)
		{
			float2 toTarget = targetPosition - selfPose.Position;
			float distance = math.length(toTarget);

			float minDistance = math.max(0f, preferredDistance - distanceTolerance);
			float maxDistance = preferredDistance + distanceTolerance;

			if (distance < 0.001f)
			{
				controlIntent.Turn = 0f;
				controlIntent.Thrust = 0f;
				return;
			}

			if (distance > maxDistance)
			{
				float desiredTheta = math.atan2(-toTarget.x, toTarget.y);
				float deltaTheta = NormalizeAngle(desiredTheta - selfPose.ThetaRad);

				controlIntent.Turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
				controlIntent.Thrust = 1f;
				return;
			}

			if (distance < minDistance)
			{
				float2 awayFromTarget = -toTarget;
				float desiredTheta = math.atan2(-awayFromTarget.x, awayFromTarget.y);
				float deltaTheta = NormalizeAngle(desiredTheta - selfPose.ThetaRad);

				controlIntent.Turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
				controlIntent.Thrust = 1f;
				return;
			}

			// Inside the preferred band: face the target, hold position.
			{
				float desiredTheta = math.atan2(-toTarget.x, toTarget.y);
				float deltaTheta = NormalizeAngle(desiredTheta - selfPose.ThetaRad);

				controlIntent.Turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
				controlIntent.Thrust = 0f;
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
