using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ShipSimSystem))] // run before movement
public partial struct ControlledShipInputSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<PlayerInput>();
		state.RequireForUpdate<ControlledShip>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var input = SystemAPI.GetSingleton<PlayerInput>();
		var controlled = SystemAPI.GetSingleton<ControlledShip>().Value;

		if (controlled == Entity.Null) return;
		if (!state.EntityManager.Exists(controlled)) return;

		// If your ship sim reads ShipSpeed/TurnSpeed + angle, we can directly drive angle and forward motion.
		// For cleanliness: store a "ShipControl" component. But minimal: just override in sim via singleton.
		// Here: write a per-ship input component on the controlled ship.

		if (!state.EntityManager.HasComponent<ShipControl>(controlled))
			state.EntityManager.AddComponentData(controlled, new ShipControl());

		state.EntityManager.SetComponentData(controlled, new ShipControl
		{
			Turn = input.Turn,
			Thrust = input.Thrust,
			Fire = input.Fire
		});
	}
}

public struct ShipControl : IComponentData
{
	public float Turn;   // -1..+1
	public float Thrust; // -1..+1
	public byte Fire;
}