using System;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	public struct ShipPrefabEntry : IBufferElementData
	{
		public ShipType Type;
		public Entity Prefab;
	}

	public class ShipPrefabReferenceAuthoring : MonoBehaviour
	{
		[Serializable]
		public struct Entry
		{
			public ShipType Type;
			public GameObject Prefab;
		}

		public Entry[] Entries;

		public class ShipPrefabReferenceBaker : Baker<ShipPrefabReferenceAuthoring>
		{
			public override void Bake(ShipPrefabReferenceAuthoring authoring)
			{
				Entity registryEntity = GetEntity(TransformUsageFlags.None);

				DynamicBuffer<ShipPrefabEntry> buffer = AddBuffer<ShipPrefabEntry>(registryEntity);
				for (int index = 0; index < authoring.Entries.Length; index++)
				{
					Entry entry = authoring.Entries[index];
					Entity prefabEntity = GetEntity(entry.Prefab, TransformUsageFlags.Dynamic);

					buffer.Add(new ShipPrefabEntry
					{
						Type = entry.Type,
						Prefab = prefabEntity
					});
				}
			}
		}
	}
}