using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Targeting
{
	// Bridges CurrentTarget → MovementTarget for AI-controlled ships.
	// Only sets the reference and mode — PreferredDistance and DistanceTolerance
	// come from MovementTargetAuthoring and are not overwritten here.
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AIIntentSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CurrentTarget>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach ((RefRO<CurrentTarget> currentTarget,
					  RefRO<TargetingProfile> targetingProfile,
					  RefRW<MovementTarget> movementTarget) in
					 SystemAPI.Query<
						 RefRO<CurrentTarget>,
						 RefRO<TargetingProfile>,
						 RefRW<MovementTarget>>()
					 .WithNone<ControlledTag>())
			{
				if (targetingProfile.ValueRO.Mode == TargetingMode.Manual)
				{
					continue;
				}

				Entity target = currentTarget.ValueRO.Value;

				if (target == Entity.Null)
				{
					movementTarget.ValueRW.Reference = TargetReference.None();
					movementTarget.ValueRW.Mode = MovementTargetMode.None;
					continue;
				}

				movementTarget.ValueRW.Reference = TargetReference.FromEntity(target);
				movementTarget.ValueRW.Mode = MovementTargetMode.ChaseAtRange;
			}
		}
	}
}
