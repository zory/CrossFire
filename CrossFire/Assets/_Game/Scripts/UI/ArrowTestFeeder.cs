using System.Collections.Generic;
using UnityEngine;

public class ArrowTestFeeder : MonoBehaviour
{
	public OffscreenArrowManager manager;

	public List<GameObject> targets = new List<GameObject>();

	void Update()
	{
		List<Vector3> targetPositions = new List<Vector3>();
		foreach (var target in targets)
		{
			targetPositions.Add(target.transform.position);
		}
		manager.SetTargets(targetPositions);
	}
}