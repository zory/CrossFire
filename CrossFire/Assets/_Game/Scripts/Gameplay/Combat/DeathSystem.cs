using CrossFire.Ships;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CrossFire
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(BulletHitSystem))]
	[BurstCompile]
	public partial struct DeathSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (healthRO, entity) in
					 SystemAPI.Query<RefRO<Health>>()
							  .WithAll<ShipTag>()
							  .WithEntityAccess())
			{
				if (healthRO.ValueRO.Value <= 0f)
					ecb.DestroyEntity(entity);
			}

			ecb.Playback(state.EntityManager);
			ecb.Dispose();
		}
	}
}
