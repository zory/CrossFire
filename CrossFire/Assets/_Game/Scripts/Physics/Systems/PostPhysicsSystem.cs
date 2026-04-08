using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Physics
{
	/// <summary>
	/// sync physics pose
	/// </summary>
	[DisableAutoCreation]
	//[UpdateInGroup(typeof(SimulationSystemGroup))]
	//[UpdateAfter(typeof(CollisionDetectionSystem))]
	//[UpdateBefore(typeof(TransformSystemGroup))]
	[BurstCompile]
	public partial struct PostPhysicsSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<WorldPose>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (pose, localTransport) in SystemAPI.Query<RefRO<WorldPose>, RefRW<LocalTransform>>())
			{
				float2 position = pose.ValueRO.Value.Position;
				localTransport.ValueRW.Position = new float3(position.x, position.y, 0f);

				float theta = pose.ValueRO.Value.ThetaRad;
				localTransport.ValueRW.Rotation = quaternion.RotateZ(theta);
			}
		}
	}
}