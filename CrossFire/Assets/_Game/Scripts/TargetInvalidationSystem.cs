using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace CrossFire.Ships
{
	/// <summary>
	/// Adds NeedsTargetTag when the current Target is invalid:
	/// - Target is Entity.Null
	/// - Target entity no longer exists
	/// - (Optional) Target has DeadTag
	/// - (Optional) Target is same TeamId (guards against bad target assignment)
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[BurstCompile]
	public partial struct TargetInvalidationSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Targetable>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

			foreach ((RefRW<Targetable> target, Entity e) in
					 SystemAPI.Query<RefRW<Targetable>>()
							  .WithEntityAccess())
			{
				var t = target.ValueRO.Value;

				bool invalid = false;

				if (t == Entity.Null || !em.Exists(t))
				{
					invalid = true;
				}
				else
				{
					// Optional: if you have a DeadTag (or Disabled), invalidate.
					//if (em.HasComponent<DeadTag>(t))
					//	invalid = true;

					// Optional: same-team guard (only if both have TeamId)
					if (!invalid &&
						em.HasComponent<TeamId>(e) &&
						em.HasComponent<TeamId>(t) &&
						em.GetComponentData<TeamId>(e).Value == em.GetComponentData<TeamId>(t).Value)
					{
						invalid = true;
					}
				}

				if (!invalid)
					continue;

				// Clear target for explicit state
				target.ValueRW.Value = Entity.Null;

				// Mark for acquisition (avoid duplicate add)
				if (!em.HasComponent<NeedsTargetTag>(e))
				{
					ecb.AddComponent<NeedsTargetTag>(e);
				}
			}

			ecb.Playback(em);
			ecb.Dispose();
		}
	}

}
