using CrossFire.Core;
using CrossFire.Physics;
using CrossFire.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.GraphicsBuffer;

namespace CrossFire.Combat
{
	// Assumed components/tags:
	// public struct NeedsTargetTag : IComponentData {}
	// public struct Target : IComponentData { public Entity Value; }
	// public struct TeamId : IComponentData { public byte Value; }
	// public struct SelectableTag : IComponentData {}    // optional filter if only some are targetable
	// public struct DeadTag : IComponentData {}          // optional
	// You are using LocalTransform elsewhere; if you use Pos instead, swap it in.

	/// <summary>
	/// For each entity that NeedsTargetTag, finds the nearest enemy and sets Target.Value.
	/// Then removes NeedsTargetTag.
	///
	/// MVP implementation: O(N^2) worst-case. Use for ~1k–2k ships.
	/// Later replace with spatial hash / broadphase.
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[BurstCompile]
	public partial struct TargetAcquireSystem : ISystem
	{
		private EntityQuery _candidatesQuery;

		public void OnCreate(ref SystemState state)
		{
			// Candidate targets: have position + TeamId, and are not dead.
			// If you don't have DeadTag or SelectableTag, remove those filters.
			_candidatesQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadOnly<WorldPose>(),
					ComponentType.ReadOnly<TeamId>(),
					ComponentType.ReadOnly<SelectableTag>(),
				},
				//None = new[]
				//{
					//ComponentType.ReadOnly<DeadTag>(),
				//}
			});

			state.RequireForUpdate<NeedsTargetTag>();
			state.RequireForUpdate(_candidatesQuery);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			// Snapshot candidates into arrays once per update.
			// (Brute force but avoids nested entity queries.)
			using var candidates = _candidatesQuery.ToEntityArray(Allocator.Temp);
			using var candXforms = _candidatesQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var candTeams = _candidatesQuery.ToComponentDataArray<TeamId>(Allocator.Temp);

			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRO<WorldPose> selfXform, RefRO<TeamId> selfTeam, RefRW<Targetable> target, Entity self) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<TeamId>, RefRW<Targetable>>()
							  .WithAll<NeedsTargetTag>()
							  .WithEntityAccess())
			{
				float2 selfPos = selfXform.ValueRO.Value.Position;

				byte team = selfTeam.ValueRO.Value;

				Entity best = Entity.Null;
				float bestDistSq = float.MaxValue;

				for (int i = 0; i < candidates.Length; i++)
				{
					Entity cand = candidates[i];

					if (cand == self)
						continue;

					if (candTeams[i].Value == team)
						continue;

					float2 p = candXforms[i].Value.Position;

					float2 d = p - selfPos;
					float dsq = math.dot(d, d);

					if (dsq < bestDistSq)
					{
						bestDistSq = dsq;
						best = cand;
					}
				}

				// Write result (explicit state)
				target.ValueRW.Value = best;

				// If no target found, keep NeedsTargetTag so we try again next frame (or remove—your choice).
				if (best != Entity.Null)
					ecb.RemoveComponent<NeedsTargetTag>(self);
			}

			ecb.Playback(em);
			ecb.Dispose();
		}
	}
}