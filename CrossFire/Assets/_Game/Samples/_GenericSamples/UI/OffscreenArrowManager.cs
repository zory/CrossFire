using CrossFire.UI;
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
	public void SetTargets(IReadOnlyList<LookupUIResult> lookupResults)
	{
		if (lookupResults == null) lookupResults = System.Array.Empty<LookupUIResult>();

		// 1) Resize pool to match count
		EnsureCount(lookupResults.Count);

		// 2) Update each arrow
		for (int i = 0; i < lookupResults.Count; i++)
			arrows[i].UpdateArrow(lookupResults[i]);
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