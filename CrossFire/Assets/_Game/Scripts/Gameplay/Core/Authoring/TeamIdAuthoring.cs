using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	// TeamId is overwritten at spawn time by ShipsSpawnSystem; the default
	// value here is just a prefab placeholder.
	public class TeamIdAuthoring : MonoBehaviour
	{
		public byte DefaultTeamId = 0;

		class Baker : Baker<TeamIdAuthoring>
		{
			public override void Bake(TeamIdAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new TeamId { Value = authoring.DefaultTeamId });
			}
		}
	}
}
