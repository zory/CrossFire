using Unity.Entities;
using Unity.Mathematics;

//*************
//Bake data
//*************
public struct BattleConfig : IComponentData
{
	public float CellSize; // broadphase
	public int SpawnSeed;
	public int MaxSpawnAttemptsPerShip;
}

// Buffer element: team visual/config
public struct TeamConfigElement : IBufferElementData
{
	public float4 ColorRGBA;
	public int TotalShips;
}

// Buffer element: spawn areas (AABB in world space)
public struct SpawnAreaElement : IBufferElementData
{
	public byte Team;
	public float2 Min;
	public float2 Max;
}


//Ship
public struct ShipPrefabRef : IComponentData
{
	public Entity Value;
}

public struct ShipTag : IComponentData
{
}

public struct TurnSpeed : IComponentData
{
	// radians per second
	public float Value;
}

public struct ThrustAcceleration : IComponentData
{
	public float Value;
}

public struct BrakeAcceleleration : IComponentData
{
	public float Value;
}

public struct MaxSpeed : IComponentData
{
	public float Value;
}

public struct ShootCooldown : IComponentData
{
	public float Value;
}

public struct ShootSpeed : IComponentData
{
	public float Value;
}


//Bulet
public struct BulletPrefabRef : IComponentData
{
	public Entity Value;
}

public struct BulletTag : IComponentData
{ 
}

public struct BulletLifetime : IComponentData
{
	public float Seconds;
}

public struct BulletDamage : IComponentData
{
	public short Value;
}


//Generic
public struct TeamId : IComponentData
{
	public byte Value;
}

public struct Pos : IComponentData
{
	public float2 Value;
}

public struct PrevPos : IComponentData
{
	public float2 Value;
}

public struct Angle : IComponentData
{
	// radians, 0 faces +Y, + is CCW
	public float Value;
}

public struct Velocity : IComponentData
{
	public float2 Value;
}

public struct Size : IComponentData
{
	public float2 Value;
}

public struct CollisionRadius : IComponentData
{
	public float Value;
}

public struct Health : IComponentData
{
	public short Value;
}
















// Singleton: player position (written by bridge MonoBehaviour)
public struct PlayerPos : IComponentData
{
	public float2 Value;
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





