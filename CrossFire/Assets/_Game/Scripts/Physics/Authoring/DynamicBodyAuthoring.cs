using Unity.Entities;
using UnityEngine;

namespace Core.Physics
{
	public class DynamicBodyAuthoring : MonoBehaviour
	{
		class Baker : Baker<DynamicBodyAuthoring>
		{
			public override void Bake(DynamicBodyAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<Velocity>(prefabEntity);
				AddComponent<AngularVelocity>(prefabEntity);
			}
		}
	}
}