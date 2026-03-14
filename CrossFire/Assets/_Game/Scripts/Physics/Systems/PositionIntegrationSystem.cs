using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	/// <summary>
	/// Moves stuff
	/// </summary>
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(AngularIntegrationSystem))]
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct PositionIntegrationSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
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