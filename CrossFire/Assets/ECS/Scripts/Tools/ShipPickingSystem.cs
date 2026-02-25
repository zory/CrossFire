//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using UnityEngine;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial struct ShipPickingSystem : ISystem
//{
//	EntityQuery _shipQuery;

//	public void OnCreate(ref SystemState state)
//	{
//		// Ships must exist for this to work
//		state.RequireForUpdate<ShipTag>();

//		_shipQuery = state.GetEntityQuery(
//			ComponentType.ReadOnly<ShipTag>(),
//			ComponentType.ReadOnly<ShipPos>());

//		// Ensure singleton exists (stores current selection)
//		if (!SystemAPI.HasSingleton<SelectedEntity>())
//		{
//			var e = state.EntityManager.CreateEntity(typeof(SelectedEntity));
//			state.EntityManager.SetComponentData(e, new SelectedEntity { Value = Entity.Null });
//		}
//	}

//	public void OnUpdate(ref SystemState state)
//	{
//		// Only react on click
//		if (!Input.GetMouseButtonDown(0))
//			return;

//		var cam = Camera.main;
//		if (cam == null) return;

//		float3 mw = cam.ScreenToWorldPoint(Input.mousePosition);
//		float2 mouseWorld = new float2(mw.x, mw.y);

//		// Pick radius in world units (tune)
//		float pickRadius = 1.0f;
//		float pickRadiusSq = pickRadius * pickRadius;

//		Entity best = Entity.Null;
//		float bestDsq = float.MaxValue;

//		// This loop runs only on click. For 50k ships this is fine for debugging.
//		foreach (var (pos, e) in SystemAPI.Query<RefRO<ShipPos>>().WithAll<ShipTag>().WithEntityAccess())
//		{
//			float2 p = pos.ValueRO.Value;
//			float dsq = math.lengthsq(p - mouseWorld);
//			if (dsq <= pickRadiusSq && dsq < bestDsq)
//			{
//				bestDsq = dsq;
//				best = e;
//			}
//		}

//		if (best == Entity.Null)
//			return;

//		// Remove SelectedTag from previous selection, add to new selection
//		var em = state.EntityManager;
//		var selSingletonEntity = SystemAPI.GetSingletonEntity<SelectedEntity>();
//		var current = SystemAPI.GetSingleton<SelectedEntity>().Value;

//		if (current != Entity.Null && em.Exists(current) && em.HasComponent<SelectedTag>(current))
//			em.RemoveComponent<SelectedTag>(current);

//		if (!em.HasComponent<SelectedTag>(best))
//			em.AddComponent<SelectedTag>(best);

//		em.SetComponentData(selSingletonEntity, new SelectedEntity { Value = best });

//		// Optional: log entity id so you can search by ID too
//		// Debug.Log($"Selected entity: {best}");
//	}
//}