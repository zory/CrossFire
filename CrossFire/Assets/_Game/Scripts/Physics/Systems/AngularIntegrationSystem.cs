using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	/// <summary>
	/// rotates stuff
	/// </summary>
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(LinearDampingSystem))]
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct AngularIntegrationSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;

			foreach (var (poseRW, angularVelRO) in SystemAPI.Query<RefRW<WorldPose>, RefRO<AngularVelocity>>())
			{
				Pose2D pose = poseRW.ValueRO.Value;
				pose.ThetaRad += angularVelRO.ValueRO.Value * deltaTime;
				poseRW.ValueRW.Value = pose;
			}
		}
	}
}