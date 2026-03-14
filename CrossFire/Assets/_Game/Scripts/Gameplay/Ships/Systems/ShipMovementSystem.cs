using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ShipMovementSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach ((RefRO<WorldPose> worldPose, RefRW<Velocity> velocity, RefRW<AngularVelocity> angularVelocity, RefRO<ControlIntent> controlIntent, RefRO<TurnSpeed> turnSpeed, RefRO<ThrustAcceleration> thrustAcceleration, RefRO<BrakeAcceleration> brakeAcceleration) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRW<Velocity>, RefRW<AngularVelocity>, RefRO<ControlIntent>, RefRO<TurnSpeed>, RefRO<ThrustAcceleration>, RefRO<BrakeAcceleration>>())
			{
				Pose2D pose = worldPose.ValueRO.Value;
				float2 currentVelocity = velocity.ValueRO.Value;

				float turn = math.clamp(controlIntent.ValueRO.Turn, -1f, 1f);
				angularVelocity.ValueRW.Value = turn * turnSpeed.ValueRO.Value;

				float2 forward = new float2(-math.sin(pose.ThetaRad), math.cos(pose.ThetaRad));

				float thrust = math.clamp(controlIntent.ValueRO.Thrust, -1f, 1f);
				if (math.abs(thrust) > 1e-4f)
				{
					currentVelocity += forward * (thrustAcceleration.ValueRO.Value * thrust) * deltaTime;
				}
				else
				{
					float speed = math.length(currentVelocity);
					if (speed > 1e-4f)
					{
						float deceleration = brakeAcceleration.ValueRO.Value * deltaTime;
						float newSpeed = math.max(0f, speed - deceleration);
						currentVelocity *= newSpeed / speed;
					}
				}

				velocity.ValueRW.Value = currentVelocity;
			}
		}
	}
}