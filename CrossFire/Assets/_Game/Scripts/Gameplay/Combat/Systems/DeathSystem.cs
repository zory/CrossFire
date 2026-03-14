using CrossFire.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct DeathSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRO<Health> health, Entity entity) in
					 SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
			{
				if (health.ValueRO.Value <= 0)
				{
					entityCommandBuffer.DestroyEntity(entity);
				}
			}

			entityCommandBuffer.Playback(state.EntityManager);
			entityCommandBuffer.Dispose();
		}
	}
}