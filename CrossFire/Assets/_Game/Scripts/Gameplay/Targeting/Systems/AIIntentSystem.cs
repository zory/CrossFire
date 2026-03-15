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
		private const float NavigationArrivalDistance = 0.5f;
		private const float FireRange = 10f;
		private const float FireConeCos = 0.98f;

		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<NavigationSolution>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach ((RefRO<WorldPose> selfPose,
					  RefRW<ControlIntent> controlIntent,
					  RefRO<NavigationSolution> navigationSolution,
					  DynamicBuffer<WeaponAimSolution> weaponAimSolutions) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRW<ControlIntent>, RefRO<NavigationSolution>, DynamicBuffer<WeaponAimSolution>>()
						.WithNone<ControlledTag>())
			{
				Pose2D self = selfPose.ValueRO.Value;
				float2 selfPosition = self.Position;
				float2 selfForward = new float2(-math.sin(self.ThetaRad), math.cos(self.ThetaRad));

				bool hasNav = navigationSolution.ValueRO.HasSolution != 0;
				bool hasWeaponAim = false;
				float2 preferredAimPoint = selfPosition;

				for (int index = 0; index < weaponAimSolutions.Length; index++)
				{
					if (weaponAimSolutions[index].HasSolution == 0)
					{
						continue;
					}

					hasWeaponAim = true;
					preferredAimPoint = weaponAimSolutions[index].AimPoint;
					break;
				}

				float2 steeringTarget = selfPosition;
				bool hasSteeringTarget = false;

				if (hasNav)
				{
					steeringTarget = navigationSolution.ValueRO.Destination;
					hasSteeringTarget = true;
				}
				else if (hasWeaponAim)
				{
					steeringTarget = preferredAimPoint;
					hasSteeringTarget = true;
				}

				if (!hasSteeringTarget)
				{
					controlIntent.ValueRW.Turn = 0f;
					controlIntent.ValueRW.Thrust = 0f;
					controlIntent.ValueRW.Fire = 0;
					continue;
				}

				float2 toSteeringTarget = steeringTarget - selfPosition;
				float distanceSq = math.lengthsq(toSteeringTarget);

				if (distanceSq < 0.0001f)
				{
					controlIntent.ValueRW.Turn = 0f;
					controlIntent.ValueRW.Thrust = 0f;
				}
				else
				{
					float desiredTheta = math.atan2(-toSteeringTarget.x, toSteeringTarget.y);
					float deltaTheta = NormalizeAngle(desiredTheta - self.ThetaRad);

					float turn = math.clamp(deltaTheta * 2.0f, -1f, 1f);
					float thrust = 0f;

					if (hasNav)
					{
						if (distanceSq > NavigationArrivalDistance * NavigationArrivalDistance)
						{
							thrust = 1f;
						}
					}
					else
					{
						thrust = 1f;
					}

					controlIntent.ValueRW.Turn = turn;
					controlIntent.ValueRW.Thrust = thrust;
				}

				byte fire = 0;

				for (int index = 0; index < weaponAimSolutions.Length; index++)
				{
					WeaponAimSolution weaponSolution = weaponAimSolutions[index];

					if (weaponSolution.HasSolution == 0)
					{
						continue;
					}

					float2 toAimPoint = weaponSolution.AimPoint - selfPosition;
					float weaponDistanceSq = math.lengthsq(toAimPoint);

					if (weaponDistanceSq < 0.0001f)
					{
						continue;
					}

					float2 aimDirection = math.normalizesafe(toAimPoint);
					float facingDot = math.dot(selfForward, aimDirection);

					if (weaponDistanceSq <= FireRange * FireRange && facingDot >= FireConeCos)
					{
						fire = 1;
						break;
					}
				}

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