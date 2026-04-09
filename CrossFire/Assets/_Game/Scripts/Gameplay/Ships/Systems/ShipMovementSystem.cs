using CrossFire.Core;
using Core.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Ships
{
	/// <summary>
	/// Translates <see cref="ControlIntent"/> into <see cref="Velocity"/> and
	/// <see cref="AngularVelocity"/> changes each frame.
	///
	/// Turn intent is applied directly as angular velocity (instant steering model).
	/// Thrust intent applies linear acceleration along the ship's forward vector:
	/// positive values use <see cref="ThrustAcceleration"/>, negative values use
	/// <see cref="BrakeAcceleration"/>, allowing reverse/deceleration at a different rate.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Movement — runs after all intent systems have written
	/// <see cref="ControlIntent"/>, and before velocity-integration systems
	/// (LinearDampingSystem → AngularIntegrationSystem → PositionIntegrationSystem).
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ShipMovementSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, Velocity, AngularVelocity, ControlIntent,
				         TurnSpeed, ThrustAcceleration, BrakeAcceleration>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (poseRO, velRW, angularVelRW, intentRO, turnSpeedRO, thrustAccRO, brakeAccRO) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRW<Velocity>, RefRW<AngularVelocity>,
					                 RefRO<ControlIntent>, RefRO<TurnSpeed>,
					                 RefRO<ThrustAcceleration>, RefRO<BrakeAcceleration>>())
			{
				float turn = math.clamp(intentRO.ValueRO.Turn, -1f, 1f);
				angularVelRW.ValueRW.Value = turn * turnSpeedRO.ValueRO.Value;

				Pose2D pose = poseRO.ValueRO.Value;
				float2 forward = new float2(-math.sin(pose.ThetaRad), math.cos(pose.ThetaRad));

				float thrust = math.clamp(intentRO.ValueRO.Thrust, -1f, 1f);
				if (math.abs(thrust) > 1e-4f)
				{
					float acceleration = thrust > 0f
						? thrustAccRO.ValueRO.Value
						: brakeAccRO.ValueRO.Value;
					velRW.ValueRW.Value += forward * (acceleration * thrust) * deltaTime;
				}
			}
		}
	}
}
