using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.Bullets
{
	public class BulletPrefabAuthoring : MonoBehaviour
	{
		class Baker : Baker<BulletPrefabAuthoring>
		{
			public short BulletDamage = 1;
			public float CollisionRadius = 0.25f;
			public override void Bake(BulletPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<PrevWorldPose>(prefabEntity);
				AddComponent<WorldPose>(prefabEntity);
				AddComponent<LocalTransform>(prefabEntity);
				AddComponent<Velocity>(prefabEntity);
				AddComponent<URPMaterialPropertyBaseColor>(prefabEntity);
				AddComponent<BulletTag>(prefabEntity);
				AddComponent<Lifetime>(prefabEntity);
				AddComponent<BulletOwner>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<BulletDamage>(prefabEntity, new BulletDamage() { Value = BulletDamage });
				AddComponent<CollisionRadius>(prefabEntity, new CollisionRadius() { Value = CollisionRadius });
			}
		}
	}
}