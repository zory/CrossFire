using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	public struct ShipPrefabReference : IComponentData
	{
		public Entity Prefab;
	}

	public class ShipPrefabReferenceAuthoring : MonoBehaviour
	{
		public GameObject ShipPrefab;

		public class ShipPrefabReferenceBaker : Baker<ShipPrefabReferenceAuthoring>
		{
			public override void Bake(ShipPrefabReferenceAuthoring authoring)
			{
				Entity holderEntity = GetEntity(TransformUsageFlags.None);
				Entity prefabEntity = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic);

				AddComponent(holderEntity, new ShipPrefabReference
				{
					Prefab = prefabEntity
				});
			}
		}
	}
}