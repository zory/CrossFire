using CrossFire.Core;
using CrossFire.Physics;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Combat
{
	public class BulletPrefabAuthoring : MonoBehaviour
	{
		public short BulletDamage = 1;

		class Baker : Baker<BulletPrefabAuthoring>
		{
			public override void Bake(BulletPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<BulletTag>(prefabEntity);

				AddComponent<StableId>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<Owner>(prefabEntity);

				AddComponent<Lifetime>(prefabEntity);

				AddComponent<BulletDamage>(prefabEntity, new BulletDamage() { Value = authoring.BulletDamage });
				AddComponent<CollisionLayer>(prefabEntity, new CollisionLayer() { Value = 1 });
				AddComponent<CollisionMask>(prefabEntity, new CollisionMask() { Value = (1 << 0) | (1 << 1) });
			}
		}
	}
}