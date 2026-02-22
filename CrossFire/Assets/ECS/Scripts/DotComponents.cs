using Unity.Entities;
using Unity.Mathematics;

public struct ShipTag : IComponentData { }

public struct TeamId : IComponentData
{
	public byte Value;
}

public struct ShipPos : IComponentData
{
	public float2 Value;
}

public struct ShipPrevPos : IComponentData
{
	public float2 Value;
}

public struct ShipAngle : IComponentData
{
	// radians, 0 faces +Y, + is CCW
	public float Value;
}

public struct ShipSpeed : IComponentData
{
	public float Value;
}

public struct ShipTurnSpeed : IComponentData
{
	// radians per second
	public float Value;
}

public struct ShipRadius : IComponentData
{
	public float Value;
}

// Singleton: player position (written by bridge MonoBehaviour)
public struct PlayerPos : IComponentData
{
	public float2 Value;
}

// Singleton battle config (baked)
public struct BattleConfig : IComponentData
{
	public int TeamCount;
	public int TotalShips;

	public float ShipRadius;
	public float ShipSpeed;
	public float ShipTurnSpeedRad;

	public float CellSize; // broadphase
	public float2 ShipSizeXY; // for nonuniform scaling

	public int SpawnSeed;
	public int MaxSpawnAttemptsPerShip;
}

// Buffer element: team visual/config
public struct TeamConfigElement : IBufferElementData
{
	public float4 ColorRGBA; // URP BaseColor
}

// Buffer element: spawn areas (AABB in world space)
public struct SpawnAreaElement : IBufferElementData
{
	public byte Team;
	public float2 Min;
	public float2 Max;
}

public struct ControlledShip : IComponentData
{
	public Entity Value;
}

public struct PlayerInput : IComponentData
{
	// -1..+1 turn, -1..+1 thrust
	public float Turn;
	public float Thrust;
	public byte Fire;
}