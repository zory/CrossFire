using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using CrossFire.Physics;

namespace CrossFire.Bullets
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(WeaponFireSystem))]
	[UpdateBefore(typeof(PositionIntegrationSystem))]
	[BurstCompile]
	public partial struct BulletUpdateSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (lifeRW, entity) in
					 SystemAPI.Query<RefRW<Lifetime>>()
							  .WithAll<BulletTag>()
							  .WithEntityAccess())
			{
				float t = lifeRW.ValueRO.TimeLeft - dt;
				lifeRW.ValueRW.TimeLeft = t;

				if (t <= 0f)
					ecb.DestroyEntity(entity);
			}

			ecb.Playback(state.EntityManager);
			ecb.Dispose();
		}
	}
}