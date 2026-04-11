using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	public class HealthAuthoring : MonoBehaviour
	{
		public short MaxHealth = 3;

		class Baker : Baker<HealthAuthoring>
		{
			public override void Bake(HealthAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new Health { Value = authoring.MaxHealth });
			}
		}
	}
}
