using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Targeting
{
	public class TargetingAuthoring : MonoBehaviour
	{
		class Baker : Baker<TargetingAuthoring>
		{
			public override void Bake(TargetingAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent(entity, new NavigationTarget
				{
					Value = TargetReference.None()
				});

				AddComponent(entity, new NavigationSolution
				{
					Destination = default,
					HasSolution = 0
				});

				AddBuffer<WeaponTarget>(entity);
				AddBuffer<WeaponAimSolution>(entity);
			}
		}
	}
}