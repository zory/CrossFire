using UnityEngine;

public class TeamSpawnArea : MonoBehaviour
{
	[Range(0, 31)] public int teamId = 0;
	public BoxCollider2D box;

	void Reset()
	{
		box = GetComponent<BoxCollider2D>();
	}

	public Bounds Bounds => box.bounds;

	void OnDrawGizmos()
	{
		if (!box) box = GetComponent<BoxCollider2D>();
		if (!box) return;

		Gizmos.color = Color.white;
		var b = box.bounds;
		Gizmos.DrawWireCube(b.center, b.size);
	}
}