using CrossFire.Core;
using Core.Physics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships
{
	[DisableAutoCreation]
	//[BurstCompile]
	public partial struct ShipMovementSystem : ISystem
	{
		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (poseRO, velRW, angularVelRW, intentRO, turnSpeedRO, thrustAccRO, brakeAccRO) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRW<Velocity>, RefRW<AngularVelocity>, RefRO<ControlIntent>, RefRO<TurnSpeed>, RefRO<ThrustAcceleration>, RefRO<BrakeAcceleration>>())
			{
				Pose2D pose = poseRO.ValueRO.Value;
				float2 currentVelocity = velRW.ValueRO.Value;

				float turn = math.clamp(intentRO.ValueRO.Turn, -1f, 1f);
				angularVelRW.ValueRW.Value = turn * turnSpeedRO.ValueRO.Value;

				float2 forward = new float2(-math.sin(pose.ThetaRad), math.cos(pose.ThetaRad));

				float forwardThrust = math.clamp(intentRO.ValueRO.Thrust, -1f, 1f);
				float thrust = forwardThrust;
				float reverseThrust = math.clamp(intentRO.ValueRO.Thrust, -1f, 1f);
				if (math.abs(thrust) <= 1e-4f)
				{
					thrust = reverseThrust;
				}
				if (math.abs(thrust) > 1e-4f)
				{
					currentVelocity += forward * (thrustAccRO.ValueRO.Value * thrust) * deltaTime;
				}

				velRW.ValueRW.Value = currentVelocity;
			}
		}
	}
}