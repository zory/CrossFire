using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	/// <summary>
	/// Applies exponential velocity decay (drag) each tick: <c>velocity *= exp(-damping * dt)</c>.
	/// Negative damping values are clamped to zero so the system can never accelerate a body.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Movement — runs after all intent systems have written their
	/// <see cref="Velocity"/> contributions and before <see cref="AngularIntegrationSystem"/>
	/// and <see cref="PositionIntegrationSystem"/>. In a new application, register it as the
	/// first step of the physics integration chain.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct LinearDampingSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<Velocity, LinearDamping>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (velRW, dampingRO) in SystemAPI.Query<RefRW<Velocity>, RefRO<LinearDamping>>())
			{
				float damping = math.max(0f, dampingRO.ValueRO.Value);
				velRW.ValueRW.Value *= math.exp(-damping * deltaTime);
			}
		}
	}
}
