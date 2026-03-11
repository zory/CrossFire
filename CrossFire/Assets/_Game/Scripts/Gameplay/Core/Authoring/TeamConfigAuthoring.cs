using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	public class TeamConfigAuthoring : MonoBehaviour
	{
		public Color[] TeamColors;

		class Baker : Baker<TeamConfigAuthoring>
		{
			public override void Bake(TeamConfigAuthoring authoring)
			{
				var entity = GetEntity(TransformUsageFlags.None);

				var buffer = AddBuffer<TeamColor>(entity);

				if (authoring.TeamColors != null)
				{
					for (int i = 0; i < authoring.TeamColors.Length; i++)
					{
						var c = authoring.TeamColors[i];
						buffer.Add(new TeamColor
						{
							Value = new Unity.Mathematics.float4(c.r, c.g, c.b, c.a)
						});
					}
				}
			}
		}
	}
}