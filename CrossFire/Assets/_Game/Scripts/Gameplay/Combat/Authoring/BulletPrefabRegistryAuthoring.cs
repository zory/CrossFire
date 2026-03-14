using System;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Combat
{
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
				Entity entity = GetEntity(TransformUsageFlags.None);
				DynamicBuffer<BulletPrefabEntry> buffer = AddBuffer<BulletPrefabEntry>(entity);

				if (authoring.Entries == null)
				{
					return;
				}

				for (int index = 0; index < authoring.Entries.Length; index++)
				{
					Entry entry = authoring.Entries[index];

					if (entry.Prefab == null)
					{
						continue;
					}

					buffer.Add(new BulletPrefabEntry()
					{
						Type = entry.Type,
						Prefab = GetEntity(entry.Prefab, TransformUsageFlags.Dynamic)
					});
				}
			}
		}
	}
}