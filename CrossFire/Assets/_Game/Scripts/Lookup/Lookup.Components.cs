using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Lookup
{
	// Filter singleton: Team = -1 => all, otherwise only that team.
	public struct LookupFilter : IComponentData
	{
		public int Team; // -1 = all, else 0..255
	}

	// Results singleton tag
	public struct LookupResultsTag : IComponentData { }

	// One row per ship for UI
	public struct LookupResult : IBufferElementData
	{
		public int StableId;
		public byte Team;
		public float2 WorldPos;
	}

	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct LookupBootstrapSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			var em = state.EntityManager;

			// Filter singleton
			{
				var e = em.CreateEntity();
				em.AddComponentData(e, new LookupFilter { Team = -1 }); // default: all
			}

			// Results singleton
			{
				var e = em.CreateEntity();
				em.AddComponent<LookupResultsTag>(e);
				em.AddBuffer<LookupResult>(e);
			}

			state.Enabled = false;
		}

		public void OnUpdate(ref SystemState state) { }
	}

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct LookupSnapshotSystem : ISystem
	{
		private EntityQuery _shipsQuery;

		public void OnCreate(ref SystemState state)
		{
			_shipsQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<ShipTag>(),
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<TeamId>(),
				ComponentType.ReadOnly<StableId>()
			);

			state.RequireForUpdate(_shipsQuery);
			state.RequireForUpdate<LookupFilter>();
			state.RequireForUpdate<LookupResultsTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			// read filter
			var filter = SystemAPI.GetSingleton<LookupFilter>();
			int teamFilter = filter.Team; // -1 => all

			// get results buffer
			Entity resultsEntity = GetSingletonEntity<LookupResultsTag>(em);
			var outBuf = em.GetBuffer<LookupResult>(resultsEntity);
			outBuf.Clear();

			// snapshot ships (no managed allocations)
			using var entities = _shipsQuery.ToEntityArray(Allocator.Temp);
			using var poses = _shipsQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using var teams = _shipsQuery.ToComponentDataArray<TeamId>(Allocator.Temp);
			using var ids = _shipsQuery.ToComponentDataArray<StableId>(Allocator.Temp);

			// pre-size (optional, reduces realloc)
			outBuf.EnsureCapacity(outBuf.Length + entities.Length);

			for (int i = 0; i < entities.Length; i++)
			{
				byte t = teams[i].Value;
				if (teamFilter >= 0 && t != (byte)teamFilter)
					continue;

				Pose2D p = poses[i].Value;

				outBuf.Add(new LookupResult
				{
					StableId = ids[i].Value,
					Team = t,
					WorldPos = p.Position,
				});
			}
		}

		private static Entity GetSingletonEntity<T>(EntityManager em) where T : unmanaged, IComponentData
		{
			using var q = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
			return q.GetSingletonEntity();
		}
	}
}
