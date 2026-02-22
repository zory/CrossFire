using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShipSnapshotSystem))] // use ShipPrevPos snapshot for collisions
public partial struct BulletMoveAndHitSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<ShipTag>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		float dt = SystemAPI.Time.DeltaTime;

		var shipPrev = SystemAPI.GetComponentLookup<ShipPrevPos>(true);
		var shipRadius = SystemAPI.GetComponentLookup<ShipRadius>(true);
		var shipHp = SystemAPI.GetComponentLookup<ShipHp>(false);

		// Build a ship list once (cheap enough for now; optimize later with grid)
		var ships = SystemAPI.QueryBuilder()
			.WithAll<ShipTag>()
			.Build()
			.ToEntityArray(Allocator.Temp);

		var ecb = new EntityCommandBuffer(Allocator.Temp);

		// Iterate bullets
		foreach (var (vel, life, radius, xform, bulletEntity) in
				 SystemAPI.Query<RefRO<BulletVelocity>, RefRW<BulletLifetime>, RefRO<ShipRadius>, RefRW<LocalTransform>>()
						  .WithAll<BulletTag>()
						  .WithEntityAccess())
		{
			var lt = xform.ValueRW;
			float3 p3 = lt.Position;
			float2 p = new float2(p3.x, p3.y);

			// Move
			p += vel.ValueRO.Value * dt;
			lt.Position = new float3(p.x, p.y, p3.z);
			xform.ValueRW = lt;

			// Lifetime
			life.ValueRW.Seconds -= dt;
			if (life.ValueRW.Seconds <= 0f)
			{
				ecb.DestroyEntity(bulletEntity);
				continue;
			}

			// Hit test vs ships
			float bulletR = radius.ValueRO.Value;

			Entity hit = Entity.Null;

			for (int i = 0; i < ships.Length; i++)
			{
				var s = ships[i];
				if (!shipPrev.HasComponent(s) || !shipHp.HasComponent(s) || !shipRadius.HasComponent(s)) continue;

				float2 sp = shipPrev[s].Value;
				float sr = shipRadius[s].Value;
				float rr = bulletR + sr;

				if (math.lengthsq(p - sp) <= rr * rr)
				{
					hit = s;
					break;
				}
			}

			if (hit != Entity.Null)
			{
				// Apply damage
				var hp = shipHp[hit];
				hp.Value -= 1;
				shipHp[hit] = hp;

				// Destroy bullet
				ecb.DestroyEntity(bulletEntity);

				// Destroy ship if dead
				if (hp.Value <= 0)
					ecb.DestroyEntity(hit);
			}
		}

		ecb.Playback(state.EntityManager);
		ecb.Dispose();
		ships.Dispose();
	}
}