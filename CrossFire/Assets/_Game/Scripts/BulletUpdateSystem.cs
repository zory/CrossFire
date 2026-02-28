using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.Bullets
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(WeaponFireSystem))]
	[BurstCompile]
	public partial struct BulletUpdateSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (ltRW, velRO, lifeRW, entity) in
					 SystemAPI.Query<RefRW<WorldPose>, RefRO<Velocity>, RefRW<Lifetime>>()
							  .WithAll<BulletTag>()
							  .WithEntityAccess())
			{
				// Move
				float2 p = ltRW.ValueRO.Value.Position;
				float2 v = velRO.ValueRO.Value;
				p.x += v.x * dt;
				p.y += v.y * dt;
				ltRW.ValueRW.Value.Position = p;

				// Lifetime
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