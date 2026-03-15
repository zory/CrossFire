using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct NavigationTargetResolveSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<NavigationTarget>();
		}

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			foreach ((RefRO<NavigationTarget> navigationTarget, RefRW<NavigationSolution> navigationSolution) in
					 SystemAPI.Query<RefRO<NavigationTarget>, RefRW<NavigationSolution>>())
			{
				bool hasPosition = TargetingHelpers.TryResolveTargetPosition(
					entityManager,
					navigationTarget.ValueRO.Value,
					out Unity.Mathematics.float2 destination);

				navigationSolution.ValueRW.HasSolution = (byte)(hasPosition ? 1 : 0);
				navigationSolution.ValueRW.Destination = destination;
			}
		}
	}
}