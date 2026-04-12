using System.Collections.Generic;
using Core.Physics;
using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Ships;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.App
{
	/// <summary>
	/// Reusable ECS operations for capturing and restoring gameplay simulation state.
	/// Extracted from <see cref="GameplaySimulationSerializer"/> so these primitives can
	/// be composed independently — e.g. for editor tools, unit tests, or future loaders.
	/// </summary>
	public static class GameplaySimulationOperations
	{
		// ─── Destroy ──────────────────────────────────────────────────────────

		public static void DestroyAllShips(EntityManager em)
		{
			using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<ShipTag>());
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			foreach (Entity entity in entities)
			{
				// DestroyEntity(Entity) follows LinkedEntityGroup, destroying children too.
				if (em.Exists(entity))
				{
					em.DestroyEntity(entity);
				}
			}
			entities.Dispose();
		}

		public static void DestroyAllBullets(EntityManager em)
		{
			using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BulletTag>());
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			foreach (Entity entity in entities)
			{
				if (em.Exists(entity))
				{
					em.DestroyEntity(entity);
				}
			}
			entities.Dispose();
		}

		// ─── Pose ─────────────────────────────────────────────────────────────

		/// <summary>
		/// Writes <paramref name="pose"/> into <see cref="WorldPose"/>, <see cref="PrevWorldPose"/>,
		/// and <see cref="LocalTransform"/> on <paramref name="entity"/>.
		/// </summary>
		public static void ApplyPose(EntityManager em, Entity entity, Pose2D pose)
		{
			float3     position = new float3(pose.Position.x, pose.Position.y, 0f);
			quaternion rotation = quaternion.RotateZ(pose.ThetaRad);
			em.SetComponentData(entity, new WorldPose     { Value = pose });
			em.SetComponentData(entity, new PrevWorldPose { Value = pose });
			em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, 1));
		}

		// ─── Prefab lookup ────────────────────────────────────────────────────

		/// <summary>
		/// Returns the ship prefab entity for <paramref name="type"/>, or <see cref="Entity.Null"/>
		/// if no matching entry exists in the <see cref="ShipPrefabEntry"/> registry.
		/// </summary>
		public static Entity FindShipPrefab(EntityManager em, ShipType type)
		{
			using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<ShipPrefabEntry>());
			if (query.IsEmpty)
			{
				return Entity.Null;
			}

			Entity registryEntity = query.GetSingletonEntity();
			DynamicBuffer<ShipPrefabEntry> entries = em.GetBuffer<ShipPrefabEntry>(registryEntity, isReadOnly: true);
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].Type == type)
				{
					return entries[i].Prefab;
				}
			}
			return Entity.Null;
		}

		/// <summary>
		/// Returns the bullet prefab entity for <paramref name="type"/>, or <see cref="Entity.Null"/>
		/// if no matching entry exists in the <see cref="BulletPrefabEntry"/> registry.
		/// </summary>
		public static Entity FindBulletPrefab(EntityManager em, BulletType type)
		{
			using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BulletPrefabEntry>());
			if (query.IsEmpty)
			{
				return Entity.Null;
			}

			Entity registryEntity = query.GetSingletonEntity();
			DynamicBuffer<BulletPrefabEntry> entries = em.GetBuffer<BulletPrefabEntry>(registryEntity, isReadOnly: true);
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].Type == type)
				{
					return entries[i].Prefab;
				}
			}
			return Entity.Null;
		}

		// ─── Capture ──────────────────────────────────────────────────────────

		public static ShipSaveData[] CaptureShips(EntityManager em)
		{
			using EntityQuery query = em.CreateEntityQuery(
				ComponentType.ReadOnly<ShipTag>(),
				ComponentType.ReadOnly<StableId>(),
				ComponentType.ReadOnly<ShipTypeId>(),
				ComponentType.ReadOnly<TeamId>(),
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<Velocity>(),
				ComponentType.ReadOnly<AngularVelocity>(),
				ComponentType.ReadOnly<Health>()
			);

			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			ShipSaveData[] result = new ShipSaveData[entities.Length];

			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				Pose2D pose   = em.GetComponentData<WorldPose>(entity).Value;
				float2 vel    = em.GetComponentData<Velocity>(entity).Value;

				result[i] = new ShipSaveData
				{
					StableId        = em.GetComponentData<StableId>(entity).Value,
					ShipType        = (int)em.GetComponentData<ShipTypeId>(entity).Value,
					Team            = em.GetComponentData<TeamId>(entity).Value,
					PositionX       = pose.Position.x,
					PositionY       = pose.Position.y,
					ThetaRad        = pose.ThetaRad,
					VelocityX       = vel.x,
					VelocityY       = vel.y,
					AngularVelocity = em.GetComponentData<AngularVelocity>(entity).Value,
					Health          = em.GetComponentData<Health>(entity).Value,
					WeaponCooldown  = em.HasComponent<WeaponCooldown>(entity)
					                      ? em.GetComponentData<WeaponCooldown>(entity).TimeLeft
					                      : 0f,
				};
			}

			entities.Dispose();
			return result;
		}

		public static BulletSaveData[] CaptureBullets(EntityManager em)
		{
			using EntityQuery query = em.CreateEntityQuery(
				ComponentType.ReadOnly<BulletTag>(),
				ComponentType.ReadOnly<BulletTypeId>(),
				ComponentType.ReadOnly<TeamId>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<WorldPose>(),
				ComponentType.ReadOnly<Velocity>(),
				ComponentType.ReadOnly<Lifetime>(),
				ComponentType.ReadOnly<BulletDamage>()
			);

			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			BulletSaveData[] result = new BulletSaveData[entities.Length];

			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				Pose2D pose   = em.GetComponentData<WorldPose>(entity).Value;
				float2 vel    = em.GetComponentData<Velocity>(entity).Value;

				Entity ownerEntity   = em.GetComponentData<Owner>(entity).Value;
				int    ownerStableId = -1;
				if (ownerEntity != Entity.Null
				    && em.Exists(ownerEntity)
				    && em.HasComponent<StableId>(ownerEntity))
				{
					ownerStableId = em.GetComponentData<StableId>(ownerEntity).Value;
				}

				result[i] = new BulletSaveData
				{
					BulletType        = (int)em.GetComponentData<BulletTypeId>(entity).Value,
					Team              = em.GetComponentData<TeamId>(entity).Value,
					OwnerStableId     = ownerStableId,
					PositionX         = pose.Position.x,
					PositionY         = pose.Position.y,
					ThetaRad          = pose.ThetaRad,
					VelocityX         = vel.x,
					VelocityY         = vel.y,
					LifetimeRemaining = em.GetComponentData<Lifetime>(entity).TimeLeft,
					BulletDamage      = em.GetComponentData<BulletDamage>(entity).Value,
				};
			}

			entities.Dispose();
			return result;
		}

		// ─── Spawn ────────────────────────────────────────────────────────────

		/// <summary>
		/// Instantiates and fully configures a ship entity from save data.
		/// Returns the new entity, or <see cref="Entity.Null"/> if the required prefab
		/// was not found in the registry (the ship is skipped with a warning).
		/// </summary>
		public static Entity SpawnShip(EntityManager em, ShipSaveData data)
		{
			Entity prefab = FindShipPrefab(em, (ShipType)data.ShipType);
			if (prefab == Entity.Null)
			{
				Debug.LogWarning($"[GameplaySimulationOperations] No prefab for ShipType {data.ShipType} — ship skipped.");
				return Entity.Null;
			}

			Entity entity = em.Instantiate(prefab);

			em.SetComponentData(entity, new StableId  { Value = data.StableId });
			em.SetComponentData(entity, new TeamId    { Value = data.Team });
			em.AddComponentData(entity, new ShipTypeId { Value = (ShipType)data.ShipType });

			Pose2D pose = new Pose2D
			{
				Position = new float2(data.PositionX, data.PositionY),
				ThetaRad = data.ThetaRad,
			};
			ApplyPose(em, entity, pose);

			if (em.HasComponent<Velocity>(entity))
			{
				em.SetComponentData(entity, new Velocity { Value = new float2(data.VelocityX, data.VelocityY) });
			}
			if (em.HasComponent<AngularVelocity>(entity))
			{
				em.SetComponentData(entity, new AngularVelocity { Value = data.AngularVelocity });
			}
			if (em.HasComponent<Health>(entity))
			{
				em.SetComponentData(entity, new Health { Value = data.Health });
			}
			if (em.HasComponent<WeaponCooldown>(entity))
			{
				em.SetComponentData(entity, new WeaponCooldown { TimeLeft = data.WeaponCooldown });
			}

			float4 teamColor = CoreHelpers.GetTeamColor(em, data.Team);
			em.SetComponentData(entity, new NativeColor { Value = teamColor });
			em.AddComponentData(entity, new NeedsColorRefresh { Value = teamColor });

			if (!em.HasComponent<NeedsTargetTag>(entity))
			{
				em.AddComponent<NeedsTargetTag>(entity);
			}

			FixedString64Bytes name = default;
			name.Append(new FixedString32Bytes("Ship_T"));
			name.Append((int)data.Team);
			name.Append(new FixedString32Bytes("_Id"));
			name.Append(data.StableId);
			em.SetName(entity, name);

			return entity;
		}

		/// <summary>
		/// Instantiates and fully configures a bullet entity from save data.
		/// <paramref name="stableIdToEntity"/> is used to resolve the owner ship reference.
		/// Returns the new entity, or <see cref="Entity.Null"/> if the required prefab
		/// was not found in the registry (the bullet is skipped with a warning).
		/// </summary>
		public static Entity SpawnBullet(EntityManager em, BulletSaveData data, Dictionary<int, Entity> stableIdToEntity)
		{
			Entity prefab = FindBulletPrefab(em, (BulletType)data.BulletType);
			if (prefab == Entity.Null)
			{
				Debug.LogWarning($"[GameplaySimulationOperations] No prefab for BulletType {data.BulletType} — bullet skipped.");
				return Entity.Null;
			}

			Entity entity = em.Instantiate(prefab);

			em.AddComponentData(entity, new BulletTypeId { Value = (BulletType)data.BulletType });
			em.SetComponentData(entity, new TeamId       { Value = data.Team });
			em.SetComponentData(entity, new BulletDamage { Value = data.BulletDamage });
			em.SetComponentData(entity, new Lifetime     { TimeLeft = data.LifetimeRemaining });

			Pose2D pose = new Pose2D
			{
				Position = new float2(data.PositionX, data.PositionY),
				ThetaRad = data.ThetaRad,
			};
			ApplyPose(em, entity, pose);

			if (em.HasComponent<Velocity>(entity))
			{
				em.SetComponentData(entity, new Velocity { Value = new float2(data.VelocityX, data.VelocityY) });
			}

			if (data.OwnerStableId >= 0 && stableIdToEntity.TryGetValue(data.OwnerStableId, out Entity ownerEntity))
			{
				em.SetComponentData(entity, new Owner { Value = ownerEntity });
			}

			float4 teamColor = CoreHelpers.GetTeamColor(em, data.Team);
			if (em.HasComponent<NeedsColorRefresh>(entity))
			{
				em.SetComponentData(entity, new NeedsColorRefresh { Value = teamColor });
			}
			else
			{
				em.AddComponentData(entity, new NeedsColorRefresh { Value = teamColor });
			}

			return entity;
		}
	}
}
