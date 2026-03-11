using Unity.Entities;
using UnityEngine;

namespace CrossFire.Lookup
{
	public static class LookupBridge
	{
		// team = -1 => all
		public static bool TrySetTeamFilter(int team)
		{
			var world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated) return false;

			var em = world.EntityManager;

			Entity filterEntity;
			using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<LookupFilter>()))
			{
				if (q.CalculateEntityCount() != 1) return false;
				filterEntity = q.GetSingletonEntity();
			}

			em.SetComponentData(filterEntity, new LookupFilter { Team = team });
			return true;
		}

		public static bool TryGetResults(out DynamicBuffer<LookupResult> results)
		{
			results = default;

			var world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated) return false;

			var em = world.EntityManager;

			Entity resultsEntity;
			using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<LookupResultsTag>()))
			{
				if (q.CalculateEntityCount() != 1) return false;
				resultsEntity = q.GetSingletonEntity();
			}

			results = em.GetBuffer<LookupResult>(resultsEntity);
			return true;
		}
	}
}