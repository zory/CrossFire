using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TeamSpawnAreaAuthoring : MonoBehaviour
{
	public int TeamId = 0;

	class Baker : Baker<TeamSpawnAreaAuthoring>
	{
		public override void Bake(TeamSpawnAreaAuthoring authoring)
		{
			// Create / get a singleton entity to hold spawn area buffer
			Entity entity = GetEntity(TransformUsageFlags.None);

			// Always add the buffer on THIS entity (one area entity => buffer length 1)
			DynamicBuffer<SpawnAreaElement> buffer = AddBuffer<SpawnAreaElement>(entity);

			BoxCollider2D collider = authoring.GetComponent<BoxCollider2D>();
			Bounds bounds = collider.bounds; // world-space AABB

			buffer.Add(new SpawnAreaElement
			{
				Team = (byte)Mathf.Clamp(authoring.TeamId, 0, 255),
				Min = new Unity.Mathematics.float2(bounds.min.x, bounds.min.y),
				Max = new Unity.Mathematics.float2(bounds.max.x, bounds.max.y),
			});
		}
	}

	private void OnDrawGizmos()
	{
		BoxCollider2D collider = GetComponent<BoxCollider2D>();
		Gizmos.color = Color.cyan;
		Bounds bounds = collider.bounds;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.DrawWireCube(bounds.center, bounds.size * 0.98f);
	}
}