using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	public class SelectableTagAuthoring : MonoBehaviour
	{
		class Baker : Baker<SelectableTagAuthoring>
		{
			public override void Bake(SelectableTagAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<SelectableTag>(entity);
			}
		}
	}
}
