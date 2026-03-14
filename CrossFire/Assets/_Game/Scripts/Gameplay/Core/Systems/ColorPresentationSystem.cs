using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ColorPresentationSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<NeedsColorRefresh>();
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;
			EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

			foreach (var (needsColorRefresh, entity) in SystemAPI.Query<RefRO<NeedsColorRefresh>>().WithEntityAccess())
			{
				float4 color = needsColorRefresh.ValueRO.Value;
				CoreHelpers.SetColor(entityManager, entity, color);

				ecb.RemoveComponent<NeedsColorRefresh>(entity);
			}

			ecb.Playback(entityManager);
			ecb.Dispose();
		}
	}
}