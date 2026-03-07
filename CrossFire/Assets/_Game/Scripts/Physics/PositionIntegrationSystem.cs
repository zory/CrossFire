using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Physics
{
	[BurstCompile]
	public partial struct PositionIntegrationSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;

			foreach (var (poseRW, velRO) in
					 SystemAPI.Query<RefRW<WorldPose>, RefRO<Velocity>>())
			{
				var pose = poseRW.ValueRO.Value;
				pose.Position += velRO.ValueRO.Value * dt;
				poseRW.ValueRW.Value = pose;
			}
		}
	}
}