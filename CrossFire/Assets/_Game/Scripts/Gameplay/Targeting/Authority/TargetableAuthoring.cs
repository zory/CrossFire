using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Targeting
{
    public class TargetableAuthoring : MonoBehaviour
    {
		class Baker : Baker<TargetableAuthoring>
		{
			public override void Bake(TargetableAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<TargetableTag>(prefabEntity);
			}
		}
	}
}
