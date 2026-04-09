using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	/// <summary>
	/// Integrates <see cref="AngularVelocity"/> into <see cref="WorldPose.ThetaRad"/> each tick:
	/// <c>pose.ThetaRad += angularVelocity * deltaTime</c>.
	/// Only the rotation field is modified; position is left untouched.
	/// Result is wrapped to [-π, π] to prevent float32 precision loss over long play sessions.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Movement — runs after <see cref="LinearDampingSystem"/> and
	/// <see cref="MaxVelocityClampSystem"/> have finalised velocity magnitudes and before
	/// <see cref="PositionIntegrationSystem"/>.
	/// In a new application, register it as the third step of the physics integration chain.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AngularIntegrationSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, AngularVelocity>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (poseRW, angularVelRO) in SystemAPI.Query<RefRW<WorldPose>, RefRO<AngularVelocity>>())
			{
				Pose2D pose = poseRW.ValueRO.Value;
				pose.ThetaRad += angularVelRO.ValueRO.Value * deltaTime;
				// Wrap to [-π, π] to prevent float32 precision loss over long play sessions.
				pose.ThetaRad -= math.PI * 2f * math.floor((pose.ThetaRad + math.PI) / (math.PI * 2f));
				poseRW.ValueRW.Value = pose;
			}
		}
	}
}
