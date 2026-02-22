using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TeamSpawnAreaAuthoring : MonoBehaviour
{
	public int teamId = 0;

	class Baker : Baker<TeamSpawnAreaAuthoring>
	{
		public override void Bake(TeamSpawnAreaAuthoring authoring)
		{
			// Create / get a singleton entity to hold spawn area buffer
			var holder = GetEntity(TransformUsageFlags.None);

			// Always add the buffer on THIS entity (one area entity => buffer length 1)
			var buf = AddBuffer<SpawnAreaElement>(holder);

			var col = authoring.GetComponent<BoxCollider2D>();
			var b = col.bounds; // world-space AABB

			buf.Add(new SpawnAreaElement
			{
				Team = (byte)Mathf.Clamp(authoring.teamId, 0, 255),
				Min = new Unity.Mathematics.float2(b.min.x, b.min.y),
				Max = new Unity.Mathematics.float2(b.max.x, b.max.y),
			});
		}
	}

	void OnDrawGizmos()
	{
		var col = GetComponent<BoxCollider2D>();
		if (!col) return;
		Gizmos.color = Color.white;
		var b = col.bounds;
		Gizmos.DrawWireCube(b.center, b.size);
	}
}