using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.Ships
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(CrossFire.Bullets.BulletUpdateSystem))]
	[BurstCompile]
	public partial struct BulletHitSystem : ISystem
	{
		private EntityQuery _shipsQuery;

		public void OnCreate(ref SystemState state)
		{
			_shipsQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadOnly<ShipTag>(),
					ComponentType.ReadOnly<WorldPose>(),
					ComponentType.ReadOnly<TeamId>(),
					ComponentType.ReadOnly<CollisionRadius>(),
					ComponentType.ReadWrite<Health>()
				}
			});

			state.RequireForUpdate(_shipsQuery);
			state.RequireForUpdate<BulletTag>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			// Snapshot ships once
			using var shipEntities = _shipsQuery.ToEntityArray(Allocator.Temp);
			using var shipLTs = _shipsQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var shipTeams = _shipsQuery.ToComponentDataArray<TeamId>(Allocator.Temp);
			using var shipRadii = _shipsQuery.ToComponentDataArray<CollisionRadius>(Allocator.Temp);

			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (bLT, bTeam, bRad, dmg, entity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<TeamId>, RefRO<CollisionRadius>, RefRO<BulletDamage>>()
							  .WithAll<BulletTag>()
							  .WithEntityAccess())
			{
				float2 bp = bLT.ValueRO.Value.Position;
				float br = math.max(0f, bRad.ValueRO.Value);
				float brSq = br * br;

				byte bulletTeam = bTeam.ValueRO.Value;
				short damage = dmg.ValueRO.Value;

				// Find first hit (simple)
				Entity hitShip = Entity.Null;

				for (int i = 0; i < shipEntities.Length; i++)
				{
					if (shipTeams[i].Value == bulletTeam)
						continue;

					float2 sp = shipLTs[i].Value.Position;
					float sr = math.max(0f, shipRadii[i].Value);

					float2 d = sp - bp;
					float r = sr + br;
					if (math.dot(d, d) <= r * r)
					{
						hitShip = shipEntities[i];

						// Apply damage immediately (safe: main thread). You can also buffer events.
						var h = em.GetComponentData<Health>(hitShip);
						h.Value -= damage;
						em.SetComponentData(hitShip, h);

						break;
					}
				}

				if (hitShip != Entity.Null)
				{
					// Destroy bullet on hit
					ecb.DestroyEntity(entity);
				}
			}

			ecb.Playback(em);
			ecb.Dispose();
		}
	}
}