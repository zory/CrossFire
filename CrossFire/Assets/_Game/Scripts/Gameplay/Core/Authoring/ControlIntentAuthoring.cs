using Unity.Entities;
using UnityEngine;

namespace CrossFire.Core
{
	// ControlIntent is written every frame by input/AI intent systems;
	// the baker just ensures the component slot is present on the prefab.
	public class ControlIntentAuthoring : MonoBehaviour
	{
		class Baker : Baker<ControlIntentAuthoring>
		{
			public override void Bake(ControlIntentAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<ControlIntent>(entity);
			}
		}
	}
}
