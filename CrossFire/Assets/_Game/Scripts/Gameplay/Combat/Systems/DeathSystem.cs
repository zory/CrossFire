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
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (healthRO, entity) in 
					SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
			{
				int health = healthRO.ValueRO.Value;
				if (health <= 0f)
				{
					entityCommandBuffer.DestroyEntity(entity);
				}
			}

			entityCommandBuffer.Playback(state.EntityManager);
			entityCommandBuffer.Dispose();
		}
	}
}
