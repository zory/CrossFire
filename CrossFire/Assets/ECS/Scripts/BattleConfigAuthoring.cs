using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct FactionSettings
{
	public Color Color;
	public int TotalShips;
}

public class BattleConfigAuthoring : MonoBehaviour
{
	[Header("Battle")]
	public FactionSettings[] FactionsSettings;

	[Header("Broadphase")]
	public float CellSize = 0.6f;

	[Header("Spawn")]
	public int SpawnSeed = 12345;
	public int MaxSpawnAttemptsPerShip = 25;

	[Header("Prefabs")]
	public GameObject ShipPrefab;
	public GameObject BulletPrefab;

	class Baker : Baker<BattleConfigAuthoring>
	{
		public override void Bake(BattleConfigAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.None);

			Entity prefabEntity = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic);
			AddComponent(entity,
				new ShipPrefabRef
				{
					Value = prefabEntity
				}
			);

			Entity bulletEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);
			AddComponent(entity,
				new BulletPrefabRef
				{
					Value = bulletEntity
				}
			);

			AddComponent(entity,
				new BattleConfig
				{
					CellSize = authoring.CellSize,
					SpawnSeed = authoring.SpawnSeed,
					MaxSpawnAttemptsPerShip = authoring.MaxSpawnAttemptsPerShip
				}
			);

			DynamicBuffer<TeamConfigElement> buf = AddBuffer<TeamConfigElement>(entity);
			for (int i = 0; i < authoring.FactionsSettings.Length; i++)
			{
				FactionSettings factionSettings = authoring.FactionsSettings[i];
				buf.Add(
					new TeamConfigElement
					{
						TotalShips = factionSettings.TotalShips,
						ColorRGBA = new float4(factionSettings.Color.r, factionSettings.Color.g, factionSettings.Color.b, factionSettings.Color.a)
					}
				);
			}
		}
	}
}