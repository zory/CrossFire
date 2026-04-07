using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Targeting
{
	// Sets ControlIntent.Fire for AI ships based on whether they have a valid current target.
	// Does not touch movement — ships are expected to be aimed separately.
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AIFireSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CurrentTarget>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach ((RefRO<CurrentTarget> currentTarget, RefRW<ControlIntent> controlIntent) in
					 SystemAPI.Query<RefRO<CurrentTarget>, RefRW<ControlIntent>>()
					 .WithNone<ControlledTag>())
			{
				controlIntent.ValueRW.Fire = currentTarget.ValueRO.Value != Entity.Null ? (byte)1 : (byte)0;
			}
		}
	}
}
