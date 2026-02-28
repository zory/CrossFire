using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	public struct BulletPrefabReference : IComponentData
	{
		public Entity Prefab;
	}

	public class BulletPrefabReferenceAuthoring : MonoBehaviour
	{
		public GameObject BulletPrefab;

		public class BulletPrefabReferenceBaker : Baker<BulletPrefabReferenceAuthoring>
		{
			public override void Bake(BulletPrefabReferenceAuthoring authoring)
			{
				Entity holderEntity = GetEntity(TransformUsageFlags.None);
				Entity prefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

				AddComponent(holderEntity, new BulletPrefabReference
				{
					Prefab = prefabEntity
				});
			}
		}
	}
}