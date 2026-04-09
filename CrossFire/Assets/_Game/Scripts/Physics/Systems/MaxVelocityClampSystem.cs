using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	/// <summary>
	/// Clamps the <see cref="Velocity"/> magnitude to the entity's <see cref="MaxVelocity"/> limit.
	/// Negative or zero <see cref="MaxVelocity"/> values are treated as zero, pinning the entity in place.
	/// Direction is preserved; only the magnitude is reduced.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Movement — runs after <see cref="LinearDampingSystem"/> has applied drag
	/// and before <see cref="AngularIntegrationSystem"/> and <see cref="PositionIntegrationSystem"/>,
	/// so that both integration systems always receive a velocity within the allowed limit.
	/// In a new application, register it as the second step of the physics integration chain.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct MaxVelocityClampSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<Velocity, MaxVelocity>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (velRW, maxSpeedRO) in SystemAPI.Query<RefRW<Velocity>, RefRO<MaxVelocity>>())
			{
				float2 velocity = velRW.ValueRO.Value;
				float maxVelocity = math.max(0f, maxSpeedRO.ValueRO.Value);
				float vSq = math.lengthsq(velocity);

				if (vSq > maxVelocity * maxVelocity && vSq > 1e-8f)
				{
					velRW.ValueRW.Value = velocity * (maxVelocity * math.rsqrt(vSq));
				}
			}
		}
	}
}
