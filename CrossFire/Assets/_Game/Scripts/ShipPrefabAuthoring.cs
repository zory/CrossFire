using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
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
				AddComponent<PrevWorldPose>(prefabEntity);
				AddComponent<WorldPose>(prefabEntity);
				AddComponent<LocalTransform>(prefabEntity);
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