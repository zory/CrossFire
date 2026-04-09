using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Core.Physics
{
	/// <summary>
	/// Integrates <see cref="Velocity"/> into <see cref="WorldPose.Position"/> each tick:
	/// <c>pose.Position += velocity * deltaTime</c>.
	/// Only the position field is modified; rotation is left untouched.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Movement — runs after <see cref="LinearDampingSystem"/>,
	/// <see cref="MaxVelocityClampSystem"/>, and <see cref="AngularIntegrationSystem"/> have
	/// all finalised their outputs, so position is always integrated with a fully clamped velocity.
	/// In a new application, register it as the fourth step of the physics integration chain.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct PositionIntegrationSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, Velocity>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (poseRW, velRO) in SystemAPI.Query<RefRW<WorldPose>, RefRO<Velocity>>())
			{
				Pose2D pose = poseRW.ValueRO.Value;
				pose.Position += velRO.ValueRO.Value * deltaTime;
				poseRW.ValueRW.Value = pose;
			}
		}
	}
}
