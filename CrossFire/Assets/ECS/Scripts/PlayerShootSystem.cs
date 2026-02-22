using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShipTransformSyncSystem))] // after ship transform is updated
public partial struct PlayerShootSystem : ISystem
{
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<PlayerInput>();
		state.RequireForUpdate<ControlledShip>();
		state.RequireForUpdate<BulletPrefabRef>();
	}

	public void OnUpdate(ref SystemState state)
	{
		var em = state.EntityManager;
		var input = SystemAPI.GetSingleton<PlayerInput>();
		if (input.Fire == 0) return;

		var controlled = SystemAPI.GetSingleton<ControlledShip>().Value;
		if (controlled == Entity.Null || !em.Exists(controlled)) return;
		if (!em.HasComponent<LocalTransform>(controlled)) return;
		if (!em.HasComponent<ShipAngle>(controlled)) return;

		var bulletPrefab = SystemAPI.GetSingleton<BulletPrefabRef>().Value;

		var lt = em.GetComponentData<LocalTransform>(controlled);
		float theta = em.GetComponentData<ShipAngle>(controlled).Value;

		// forward: theta=0 => +Y, +theta CCW
		float2 forward = new float2(-math.sin(theta), math.cos(theta));

		// spawn at ship nose (tune)
		float spawnOffset = 0.6f;
		float2 spawnPos2 = new float2(lt.Position.x, lt.Position.y) + forward * spawnOffset;

		// bullet speed: read from prefab default velocity? simplest: constant here
		float bulletSpeed = 25f;
		float2 bulletVel = forward * bulletSpeed;

		var b = em.Instantiate(bulletPrefab);

		// position/rotation
		em.SetComponentData(b, LocalTransform.FromPositionRotationScale(
			new float3(spawnPos2.x, spawnPos2.y, 0f),
			quaternion.RotateZ(theta),
			1f));

		// velocity
		if (em.HasComponent<BulletVelocity>(b))
			em.SetComponentData(b, new BulletVelocity { Value = bulletVel });

		// Optional: make bullets small via PostTransformMatrix if your prefab is unit-sized quad
		if (em.HasComponent<PostTransformMatrix>(b))
		{
			em.SetComponentData(b, new PostTransformMatrix
			{
				Value = float4x4.Scale(new float3(0.15f, 0.35f, 1f))
			});
		}

		// Fire is held; without a cooldown this becomes a machine gun.
		// Quick hack: consume fire for one frame
		var inputEntity = SystemAPI.GetSingletonEntity<PlayerInput>();
		em.SetComponentData(inputEntity, new PlayerInput { Turn = input.Turn, Thrust = input.Thrust, Fire = 0 });
	}
}