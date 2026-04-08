using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Physics
{
	/// <summary>
	/// apply drag
	/// </summary>
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(SnapshotSystem))]
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct LinearDampingSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
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