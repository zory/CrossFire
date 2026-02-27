using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace CrossFire.Ships
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(AIIntentSystem))]
	[BurstCompile]
	public partial struct ShipMovementSystem : ISystem
	{
		// Consider making this a component or singleton tuning later
		private const float LinearDampingPerSecond = 3f;

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (var (pose, vel, intent, maxSpeed, turnSpeed, thrustAcc, brakeAcc) in
					 SystemAPI.Query<
						 RefRW<WorldPose>, RefRW<Velocity>, RefRO<ShipIntent>,
						 RefRO<MaxSpeed>, RefRO<TurnSpeed>, RefRO<ThrustAcceleration>, RefRO<BrakeAcceleleration>>())
			{
				Pose2D p = pose.ValueRW.Value;
				float2 v = vel.ValueRW.Value;

				// Rotation
				float turn = math.clamp(intent.ValueRO.Turn, -1f, 1f);
				p.Theta += turn * turnSpeed.ValueRO.Value * dt;

				float2 forward = new float2(-math.sin(p.Theta), math.cos(p.Theta));

				// Thrust / brake
				float thrust = math.clamp(intent.ValueRO.Thrust, -1f, 1f);
				if (math.abs(thrust) > 1e-4f)
				{
					v += forward * (thrustAcc.ValueRO.Value * thrust) * dt;
				}
				else
				{
					// Brake when no thrust (your current behavior)
					float speed = math.length(v);
					if (speed > 1e-4f)
					{
						float decel = brakeAcc.ValueRO.Value * dt;
						float newSpeed = math.max(0f, speed - decel);
						v = v * (newSpeed / speed);
					}
				}

				// Linear damping (frame-rate independent)
				float d = math.max(0f, LinearDampingPerSecond);
				v *= math.exp(-d * dt);

				// Clamp
				float maxV = math.max(0f, maxSpeed.ValueRO.Value);
				float vSq = math.lengthsq(v);
				if (vSq > maxV * maxV && vSq > 1e-8f)
					v *= maxV * math.rsqrt(vSq);

				// Integrate position (drift always happens because v persists)
				p.Position += v * dt;

				vel.ValueRW.Value = v;
				pose.ValueRW.Value = p;
			}
		}
	}
}