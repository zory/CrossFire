using System;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	/// <summary>
	/// Authoring component for the ship-prefab registry.
	/// Assign one entry per <see cref="ShipType"/> in the Inspector; the baker writes a
	/// <see cref="DynamicBuffer{T}"/> of <see cref="ShipPrefabEntry"/> onto the singleton
	/// entity that <see cref="ShipsSpawnSystem"/> reads at runtime.
	/// </summary>
	public class ShipPrefabRegistryAuthoring : MonoBehaviour
	{
		[Serializable]
		public struct Entry
		{
			public ShipType Type;
			public GameObject Prefab;
		}

		public Entry[] Entries;

		class ShipPrefabRegistryBaker : Baker<ShipPrefabRegistryAuthoring>
		{
			public override void Bake(ShipPrefabRegistryAuthoring authoring)
			{
				Entity registryEntity = GetEntity(TransformUsageFlags.None);

				DynamicBuffer<ShipPrefabEntry> buffer = AddBuffer<ShipPrefabEntry>(registryEntity);

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
