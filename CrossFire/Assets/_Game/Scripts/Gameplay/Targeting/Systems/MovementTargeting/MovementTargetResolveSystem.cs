using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct MovementTargetResolveSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<MovementTarget>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			foreach ((RefRO<MovementTarget> movementTarget, RefRW<MovementTargetResolved> movementTargetResolved) in
					 SystemAPI.Query<RefRO<MovementTarget>, RefRW<MovementTargetResolved>>())
			{
				bool hasTarget = MovementHelpers.TryResolveTargetPosition(
					entityManager,
					movementTarget.ValueRO.Reference,
					out float2 worldPosition);

				movementTargetResolved.ValueRW.HasTarget = (byte)(hasTarget ? 1 : 0);
				movementTargetResolved.ValueRW.WorldPosition = worldPosition;
			}
		}
	}
}