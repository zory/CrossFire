using CrossFire.UI;
using System.Collections.Generic;
using UnityEngine;

public class ArrowTestFeeder : MonoBehaviour
{
	public OffscreenArrowManager manager;

	public List<GameObject> targets = new List<GameObject>();

	void Update()
	{
		List<LookupUIResult> targetPositions = new List<LookupUIResult>();
		foreach (var target in targets)
		{
			targetPositions.Add(
				new LookupUIResult()
				{
					WorldPos = target.transform.position,
					Team = 0,
				}
			);
		}
		manager.SetTargets(targetPositions);
	}
}