using CrossFire.Lookup;
using UnityEngine;

namespace CrossFire.UI
{
	public class LookupUI : MonoBehaviour
	{
		public int Team = -1;

		void Update()
		{
			LookupBridge.TrySetTeamFilter(Team);

			if (LookupBridge.TryGetResults(out var results))
			{
				string shipsInfo = "";
				foreach (var result in results)
				{
					shipsInfo += "StableId:" + result.StableId + " WorldPos:" + result.WorldPos + " Team:" + result.Team + "\n";
				}
				UnityEngine.Debug.Log(shipsInfo);
			}
		}
	}
}