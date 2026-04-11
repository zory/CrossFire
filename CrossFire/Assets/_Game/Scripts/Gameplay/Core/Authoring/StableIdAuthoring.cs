using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	// StableId is assigned at runtime by the spawn system; the baker just
	// ensures the component slot is present on the prefab.
	public class StableIdAuthoring : MonoBehaviour
	{
		class Baker : Baker<StableIdAuthoring>
		{
			public override void Bake(StableIdAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<StableId>(entity);
			}
		}
	}
}
