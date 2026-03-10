using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.Physics
{
	public class BasicBodyAuthoring : MonoBehaviour
	{
		class Baker : Baker<BasicBodyAuthoring>
		{
			public override void Bake(BasicBodyAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<PrevWorldPose>(prefabEntity);
				AddComponent<WorldPose>(prefabEntity);
				AddComponent<LocalTransform>(prefabEntity, LocalTransform.Identity);
			}
		}
	}
}