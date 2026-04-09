using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Physics
{
	/// <summary>
	/// Copies each entity's <see cref="WorldPose"/> into Unity's <see cref="LocalTransform"/>
	/// so the Transform system and renderer always see the physics-authoritative position and
	/// rotation.
	/// <list type="bullet">
	///   <item><see cref="WorldPose.Value"/>.Position (float2) → <see cref="LocalTransform.Position"/> (float3, z = 0)</item>
	///   <item><see cref="WorldPose.Value"/>.ThetaRad → <see cref="LocalTransform.Rotation"/> via <c>quaternion.RotateZ</c></item>
	/// </list>
	/// Scale is not touched.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Cleanup/Presentation — runs after all physics and combat systems have
	/// committed their final <see cref="WorldPose"/> values and before the Transform system
	/// propagates them to child entities and the renderer.
	/// Only entities that carry both <see cref="WorldPose"/> and <see cref="LocalTransform"/>
	/// are processed.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct PostPhysicsSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, LocalTransform>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (pose, localTransform) in SystemAPI.Query<RefRO<WorldPose>, RefRW<LocalTransform>>())
			{
				float2 position = pose.ValueRO.Value.Position;
				localTransform.ValueRW.Position = new float3(position.x, position.y, 0f);

				float theta = pose.ValueRO.Value.ThetaRad;
				localTransform.ValueRW.Rotation = quaternion.RotateZ(theta);
			}
		}
	}
}
