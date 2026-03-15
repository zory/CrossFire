using CrossFire.Core;
using CrossFire.Physics;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	public static class TargetingHelpers
	{
		public static bool TryResolveTargetPosition(
			EntityManager entityManager,
			in TargetReference targetReference,
			out float2 targetPosition)
		{
			targetPosition = float2.zero;

			if (targetReference.Kind == TargetReferenceKind.None)
			{
				return false;
			}

			if (targetReference.Kind == TargetReferenceKind.WorldPosition)
			{
				targetPosition = targetReference.WorldPosition;
				return true;
			}

			if (targetReference.Kind == TargetReferenceKind.Entity)
			{
				if (targetReference.Entity == Entity.Null)
				{
					return false;
				}

				if (!entityManager.Exists(targetReference.Entity))
				{
					return false;
				}

				if (!entityManager.HasComponent<WorldPose>(targetReference.Entity))
				{
					return false;
				}

				targetPosition = entityManager.GetComponentData<WorldPose>(targetReference.Entity).Value.Position;
				return true;
			}

			return false;
		}

		public static bool IsTargetReferenceValid(EntityManager entityManager, in TargetReference targetReference)
		{
			if (targetReference.Kind == TargetReferenceKind.None)
			{
				return false;
			}

			if (targetReference.Kind == TargetReferenceKind.WorldPosition)
			{
				return true;
			}

			if (targetReference.Kind == TargetReferenceKind.Entity)
			{
				if (targetReference.Entity == Entity.Null)
				{
					return false;
				}

				if (!entityManager.Exists(targetReference.Entity))
				{
					return false;
				}

				return true;
			}

			return false;
		}
	
		public static void ClearNavigationTarget(EntityManager entityManager, Entity shipEntity)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasComponent<NavigationTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new NavigationTarget
			{
				Value = TargetReference.None()
			});
		}

		public static void SetNavigationTargetWorld(EntityManager entityManager, Entity shipEntity, float2 worldPosition)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasComponent<NavigationTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new NavigationTarget
			{
				Value = TargetReference.FromWorldPosition(worldPosition)
			});
		}

		public static void SetNavigationTargetEntity(EntityManager entityManager, Entity shipEntity, Entity targetEntity)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasComponent<NavigationTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new NavigationTarget
			{
				Value = TargetReference.FromEntity(targetEntity)
			});
		}

		public static void ClearWeaponTargets(EntityManager entityManager, Entity shipEntity)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasBuffer<WeaponTarget>(shipEntity))
			{
				return;
			}

			entityManager.GetBuffer<WeaponTarget>(shipEntity).Clear();
		}

		public static void SetSingleWeaponTargetEntity(
			EntityManager entityManager,
			Entity shipEntity,
			byte weaponSlotIndex,
			WeaponTargetingBehavior behavior,
			Entity targetEntity)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasBuffer<WeaponTarget>(shipEntity))
			{
				return;
			}

			DynamicBuffer<WeaponTarget> buffer = entityManager.GetBuffer<WeaponTarget>(shipEntity);

			UpsertWeaponTarget(
				buffer,
				weaponSlotIndex,
				behavior,
				TargetReference.FromEntity(targetEntity));
		}

		public static void SetSingleWeaponTargetWorld(
			EntityManager entityManager,
			Entity shipEntity,
			byte weaponSlotIndex,
			WeaponTargetingBehavior behavior,
			float2 worldPosition)
		{
			if (!entityManager.Exists(shipEntity) || !entityManager.HasBuffer<WeaponTarget>(shipEntity))
			{
				return;
			}

			DynamicBuffer<WeaponTarget> buffer = entityManager.GetBuffer<WeaponTarget>(shipEntity);

			UpsertWeaponTarget(
				buffer,
				weaponSlotIndex,
				behavior,
				TargetReference.FromWorldPosition(worldPosition));
		}

		private static void UpsertWeaponTarget(
			DynamicBuffer<WeaponTarget> buffer,
			byte weaponSlotIndex,
			WeaponTargetingBehavior behavior,
			TargetReference targetReference)
		{
			for (int index = 0; index < buffer.Length; index++)
			{
				if (buffer[index].WeaponSlotIndex != weaponSlotIndex)
				{
					continue;
				}

				buffer[index] = new WeaponTarget
				{
					WeaponSlotIndex = weaponSlotIndex,
					Behavior = behavior,
					Target = targetReference
				};
				return;
			}

			buffer.Add(new WeaponTarget
			{
				WeaponSlotIndex = weaponSlotIndex,
				Behavior = behavior,
				Target = targetReference
			});
		}
	}
}