using CrossFire.Lookup;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace CrossFire.UI
{
	public class LookupUI : MonoBehaviour
	{
		public OffscreenArrowManager manager;
		public int Team = -1;

		void Update()
		{
			LookupBridge.TrySetTeamFilter(Team);

			if (LookupBridge.TryGetResults(out var results))
			{
				List<Vector3> targetPositions = new List<Vector3>();
				foreach (var result in results)
				{
					targetPositions.Add(new Vector3(result.WorldPos.x, result.WorldPos.y, 0));
				}
				manager.SetTargets(targetPositions);
			}
		}
	}
}