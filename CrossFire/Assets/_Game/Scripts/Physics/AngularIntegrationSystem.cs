using Unity.Burst;
using Unity.Entities;

namespace CrossFire.Physics
{
	[BurstCompile]
	public partial struct AngularIntegrationSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (var (poseRW, angularVelRO) in
					 SystemAPI.Query<RefRW<WorldPose>, RefRO<AngularVelocity>>())
			{
				var pose = poseRW.ValueRO.Value;
				pose.ThetaRad += angularVelRO.ValueRO.Value * dt;
				poseRW.ValueRW.Value = pose;
			}
		}
	}
}