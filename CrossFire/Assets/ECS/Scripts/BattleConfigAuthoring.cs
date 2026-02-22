using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BattleConfigAuthoring : MonoBehaviour
{
	[Header("Battle")]
	public int teamCount = 2;
	public int totalShips = 5000;

	[Header("Ship")]
	public GameObject shipPrefab;
	public GameObject bulletPrefab;
	public float shipRadius = 0.25f;
	public float shipSpeed = 5f;
	public float turnSpeedDegPerSec = 180f;
	public Vector2 shipSizeXY = new Vector2(0.35f, 0.8f);

	[Header("Broadphase")]
	public float cellSize = 0.6f;

	[Header("Spawn")]
	public int spawnSeed = 12345;
	public int maxSpawnAttemptsPerShip = 25;

	[Header("Team Colors (size must equal teamCount)")]
	public Color[] teamColors;

	class Baker : Baker<BattleConfigAuthoring>
	{
		public override void Bake(BattleConfigAuthoring authoring)
		{
			var e = GetEntity(TransformUsageFlags.None);

			var bulletEntity = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic);
			AddComponent(e, new BulletPrefabRef { Value = bulletEntity });

			var prefabEntity = GetEntity(authoring.shipPrefab, TransformUsageFlags.Dynamic);
			AddComponent(e, new ShipPrefabRef { Value = prefabEntity });

			AddComponent(e, new BattleConfig
			{
				TeamCount = authoring.teamCount,
				TotalShips = authoring.totalShips,
				ShipRadius = authoring.shipRadius,
				ShipSpeed = authoring.shipSpeed,
				ShipTurnSpeedRad = math.radians(authoring.turnSpeedDegPerSec),
				CellSize = authoring.cellSize,
				ShipSizeXY = new float2(authoring.shipSizeXY.x, authoring.shipSizeXY.y),
				SpawnSeed = authoring.spawnSeed,
				MaxSpawnAttemptsPerShip = authoring.maxSpawnAttemptsPerShip
			});

			var buf = AddBuffer<TeamConfigElement>(e);

			// Generate defaults if not provided
			if (authoring.teamColors == null || authoring.teamColors.Length != authoring.teamCount)
			{
				for (int t = 0; t < authoring.teamCount; t++)
				{
					Color c = Color.HSVToRGB((float)t / math.max(1, authoring.teamCount), 0.8f, 1f);
					buf.Add(new TeamConfigElement { ColorRGBA = new float4(c.r, c.g, c.b, c.a) });
				}
			}
			else
			{
				for (int t = 0; t < authoring.teamCount; t++)
				{
					var c = authoring.teamColors[t];
					buf.Add(new TeamConfigElement { ColorRGBA = new float4(c.r, c.g, c.b, c.a) });
				}
			}
		}
	}
}