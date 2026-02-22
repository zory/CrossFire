using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SelectedDebugDrawSystem : SystemBase
{
	protected override void OnUpdate()
	{
		if (!SystemAPI.HasSingleton<SelectedEntity>()) return;

		var sel = SystemAPI.GetSingleton<SelectedEntity>().Value;
		if (sel == Entity.Null || !EntityManager.Exists(sel)) return;
		if (!EntityManager.HasComponent<ShipPos>(sel)) return;

		float2 p = EntityManager.GetComponentData<ShipPos>(sel).Value;

		float s = 0.6f;
		Debug.DrawLine(new Vector3(p.x - s, p.y, 0), new Vector3(p.x + s, p.y, 0));
		Debug.DrawLine(new Vector3(p.x, p.y - s, 0), new Vector3(p.x, p.y + s, 0));
	}
}