using CrossFire.Lookup;
using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.UI
{
	public struct LookupUIResult
	{
		public byte Team;
		public Vector3 WorldPos;
	}

	public class LookupUI : MonoBehaviour
	{
		public OffscreenArrowManager manager;
		public int Team = -1;

		void Update()
		{
			LookupBridge.TrySetTeamFilter(Team);

			if (LookupBridge.TryGetResults(out var results))
			{
				List<LookupUIResult> lookupResults = new List<LookupUIResult>();
				foreach (var result in results)
				{
					lookupResults.Add(
						new LookupUIResult()
						{
							Team = result.Team,
							WorldPos = new Vector3(result.WorldPos.x, result.WorldPos.y, 0),
						}
					);
				}
				manager.SetTargets(lookupResults);
			}
		}
	}
}