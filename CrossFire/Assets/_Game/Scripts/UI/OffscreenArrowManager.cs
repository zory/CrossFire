using System.Collections.Generic;
using UnityEngine;

public class OffscreenArrowManager : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private Camera worldCamera;
	[SerializeField] private RectTransform boundsRect;

	[Header("Prefab + Parent")]
	[SerializeField] private OffscreenArrowItem arrowPrefab;
	[SerializeField] private RectTransform arrowsParent; // where arrows are instantiated (usually boundsRect or a child)

	private readonly List<OffscreenArrowItem> arrows = new List<OffscreenArrowItem>();

	void Awake()
	{
		if (!arrowsParent) arrowsParent = boundsRect;
	}

	/// Call this from your game with any number of world positions.
	public void SetTargets(IReadOnlyList<Vector3> worldPositions)
	{
		if (worldPositions == null) worldPositions = System.Array.Empty<Vector3>();

		// 1) Resize pool to match count
		EnsureCount(worldPositions.Count);

		// 2) Update each arrow
		for (int i = 0; i < worldPositions.Count; i++)
			arrows[i].UpdateArrow(worldPositions[i]);
	}

	private void EnsureCount(int needed)
	{
		// Create
		while (arrows.Count < needed)
		{
			var item = Instantiate(arrowPrefab, arrowsParent);
			item.Init(worldCamera, boundsRect);
			arrows.Add(item);
		}

		// Destroy extra
		for (int i = arrows.Count - 1; i >= needed; i--)
		{
			if (arrows[i]) Destroy(arrows[i].gameObject);
			arrows.RemoveAt(i);
		}
	}
}