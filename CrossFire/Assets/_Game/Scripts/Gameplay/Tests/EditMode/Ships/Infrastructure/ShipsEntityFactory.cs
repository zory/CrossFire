using CrossFire.Core;
using Core.Physics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrossFire.Ships.Tests.EditMode
{
	/// <summary>
	/// Factory helpers for creating ECS entities with common CrossFire.Ships component
	/// combinations. Keeps individual test methods free of repetitive AddComponentData calls.
	/// </summary>
	public static class ShipsEntityFactory
	{
		/// <summary>
		/// Creates the singleton entity that <see cref="ShipsSpawnSystem"/> reads commands from.
		/// Mirrors what <see cref="ShipsSpawnCommandBufferSystem"/> creates at runtime.
		/// </summary>
		public static Entity CreateSpawnCommandBuffer(EntityManager entityManager)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponent<SpawnShipsCommandBufferTag>(entity);
			entityManager.AddBuffer<SpawnShipsCommand>(entity);
			return entity;
		}

		/// <summary>
		/// Creates the singleton registry entity and registers one prefab entry mapping
		/// <paramref name="shipType"/> to <paramref name="prefabEntity"/>.
		/// </summary>
		public static Entity CreateShipPrefabRegistry(
			EntityManager entityManager,
			ShipType shipType,
			Entity prefabEntity)
		{
			Entity registryEntity = entityManager.CreateEntity();
			DynamicBuffer<ShipPrefabEntry> buffer = entityManager.AddBuffer<ShipPrefabEntry>(registryEntity);
			buffer.Add(new ShipPrefabEntry { Type = shipType, Prefab = prefabEntity });
			return registryEntity;
		}

		/// <summary>
		/// Creates a minimal ship prefab entity with all components that
		/// <see cref="ShipsSpawnSystem"/> expects to set on an instantiated ship.
		/// The entity is NOT tagged with <c>Prefab</c> — for spawn tests it can be
		/// instantiated directly via <c>EntityManager.Instantiate</c>.
		/// </summary>
		public static Entity CreateShipPrefab(EntityManager entityManager, ShipType type)
		{
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponentData(entity, new WorldPose { Value = default });
			entityManager.AddComponentData(entity, new PrevWorldPose { Value = default });
			entityManager.AddComponentData(entity, LocalTransform.Identity);
			entityManager.AddComponentData(entity, new StableId { Value = 0 });
			entityManager.AddComponentData(entity, new TeamId { Value = 0 });
			entityManager.AddComponentData(entity, new NativeColor { Value = new float4(1f, 1f, 1f, 1f) });
			return entity;
		}

		/// <summary>
		/// Creates a ship entity with all components required by <see cref="ShipMovementSystem"/>.
		/// </summary>
		public static Entity CreateShipWithMovementComponents(
			EntityManager entityManager,
			float thetaRad       = 0f,
			float thrustAcc      = 10f,
			float brakeAcc       = 5f,
			float turnSpeed      = 3f)
		{
			Entity entity = entityManager.CreateEntity();

			entityManager.AddComponentData(entity, new WorldPose
			{
				Value = new Pose2D { Position = float2.zero, ThetaRad = thetaRad }
			});
			entityManager.AddComponentData(entity, new Velocity { Value = float2.zero });
			entityManager.AddComponentData(entity, new AngularVelocity { Value = 0f });
			entityManager.AddComponentData(entity, new ControlIntent { Thrust = 0f, Turn = 0f });
			entityManager.AddComponentData(entity, new ThrustAcceleration { Value = thrustAcc });
			entityManager.AddComponentData(entity, new BrakeAcceleration { Value = brakeAcc });
			entityManager.AddComponentData(entity, new TurnSpeed { Value = turnSpeed });

			return entity;
		}

		/// <summary>
		/// Sets the <see cref="ControlIntent"/> on an existing entity.
		/// </summary>
		public static void SetIntent(
			EntityManager entityManager,
			Entity entity,
			float thrust = 0f,
			float turn   = 0f)
		{
			entityManager.SetComponentData(entity, new ControlIntent { Thrust = thrust, Turn = turn });
		}
	}
}
