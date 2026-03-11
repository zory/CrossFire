using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;
using CrossFire.Physics;
using CrossFire.Player;

namespace CrossFire.Ships
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(AIIntentSystem))]
	[UpdateBefore(typeof(PositionIntegrationSystem))]
	[UpdateBefore(typeof(PostPhysicsSystem))]
	[BurstCompile]
	public partial struct ShipMovementSystem : ISystem
	{
		// Consider making this a component or singleton tuning later
		private const float LinearDampingPerSecond = 3f;

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (var (poseRO, velRW, angularVelRW, intentRO, turnSpeedRO, thrustAccRO, brakeAccRO) in
					 SystemAPI.Query<
						 RefRO<WorldPose>,
						 RefRW<Velocity>,
						 RefRW<AngularVelocity>,
						 RefRO<ControlIntent>,
						 RefRO<TurnSpeed>,
						 RefRO<ThrustAcceleration>,
						 RefRO<BrakeAcceleration>>())
			{
				var pose = poseRO.ValueRO.Value;
				float2 v = velRW.ValueRO.Value;

				float turn = math.clamp(intentRO.ValueRO.Turn, -1f, 1f);
				angularVelRW.ValueRW.Value = turn * turnSpeedRO.ValueRO.Value;

				float2 forward = new float2(-math.sin(pose.ThetaRad), math.cos(pose.ThetaRad));

				float thrust = math.clamp(intentRO.ValueRO.Thrust, -1f, 1f);
				if (math.abs(thrust) > 1e-4f)
				{
					v += forward * (thrustAccRO.ValueRO.Value * thrust) * dt;
				}
				else
				{
					float speed = math.length(v);
					if (speed > 1e-4f)
					{
						float decel = brakeAccRO.ValueRO.Value * dt;
						float newSpeed = math.max(0f, speed - decel);
						v *= newSpeed / speed;
					}
				}

				velRW.ValueRW.Value = v;
			}
		}
	}
}