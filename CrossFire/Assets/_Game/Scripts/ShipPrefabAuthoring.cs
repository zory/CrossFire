using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace CrossFire.Ships
{
	public class ShipPrefabAuthoring : MonoBehaviour
	{
		class Baker : Baker<ShipPrefabAuthoring>
		{
			public override void Bake(ShipPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<URPMaterialPropertyBaseColor>(prefabEntity);
				AddComponent<ShipTag>(prefabEntity);
				AddComponent<StableId>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<NativeColor>(prefabEntity);
				AddComponent<SelectableTag>(prefabEntity);
			}
		}
	}
}