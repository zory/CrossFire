using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using CrossFire.Physics;

namespace CrossFire.Bullets
{
	public class BulletPrefabAuthoring : MonoBehaviour
	{
		class Baker : Baker<BulletPrefabAuthoring>
		{
			public short BulletDamage = 1;

			public override void Bake(BulletPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<URPMaterialPropertyBaseColor>(prefabEntity);
				AddComponent<BulletTag>(prefabEntity);
				AddComponent<Lifetime>(prefabEntity);
				AddComponent<BulletOwner>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<BulletDamage>(prefabEntity, new BulletDamage() { Value = BulletDamage });
				AddComponent<CollisionLayer>(prefabEntity, new CollisionLayer() { Value = 1 });
				AddComponent<CollisionMask>(prefabEntity, new CollisionMask() { Value = (1 << 0) | (1 << 1) });
			}
		}
	}
}