using CrossFire.Ships;
using System;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Combat
{
	public struct BulletPrefabEntry : IBufferElementData
	{
		public BulletType Type;
		public Entity Prefab;
	}

	public class BulletPrefabRegistryAuthoring : MonoBehaviour
	{
		[Serializable]
		public struct Entry
		{
			public BulletType Type;
			public GameObject Prefab;
		}

		public Entry[] Entries;

		public class BulletPrefabRegistryBaker : Baker<BulletPrefabRegistryAuthoring>
		{
			public override void Bake(BulletPrefabRegistryAuthoring authoring)
			{
				Entity registryEntity = GetEntity(TransformUsageFlags.None);

				DynamicBuffer<BulletPrefabEntry> buffer = AddBuffer<BulletPrefabEntry>(registryEntity);
				for (int index = 0; index < authoring.Entries.Length; index++)
				{
					Entry entry = authoring.Entries[index];
					Entity prefabEntity = GetEntity(entry.Prefab, TransformUsageFlags.Dynamic);

					buffer.Add(new BulletPrefabEntry
					{
						Type = entry.Type,
						Prefab = prefabEntity
					});
				}
			}
		}
	}
}