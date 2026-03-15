using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public static class MovementHelpers
	{
		public static void ClearMovementTarget(EntityManager entityManager, Entity shipEntity)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<MovementTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new MovementTarget
			{
				Reference = TargetReference.None(),
				Mode = MovementTargetMode.None,
				PreferredDistance = 0f,
				DistanceTolerance = 0f,
				ArrivalDistance = 0f
			});
		}

		public static void SetFlyToWorldPosition(
			EntityManager entityManager,
			Entity shipEntity,
			float2 worldPosition,
			float arrivalDistance)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<MovementTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new MovementTarget
			{
				Reference = TargetReference.FromWorldPosition(worldPosition),
				Mode = MovementTargetMode.FlyToPoint,
				PreferredDistance = 0f,
				DistanceTolerance = 0f,
				ArrivalDistance = arrivalDistance
			});
		}

		public static void SetFlyToEntity(
			EntityManager entityManager,
			Entity shipEntity,
			Entity targetEntity,
			float arrivalDistance)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<MovementTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new MovementTarget
			{
				Reference = TargetReference.FromEntity(targetEntity),
				Mode = MovementTargetMode.FlyToPoint,
				PreferredDistance = 0f,
				DistanceTolerance = 0f,
				ArrivalDistance = arrivalDistance
			});
		}

		public static void SetChaseAtRange(
			EntityManager entityManager,
			Entity shipEntity,
			Entity targetEntity,
			float preferredDistance,
			float distanceTolerance)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<MovementTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new MovementTarget
			{
				Reference = TargetReference.FromEntity(targetEntity),
				Mode = MovementTargetMode.ChaseAtRange,
				PreferredDistance = preferredDistance,
				DistanceTolerance = distanceTolerance,
				ArrivalDistance = 0f
			});
		}

		public static void SetDefendClose(
			EntityManager entityManager,
			Entity shipEntity,
			Entity targetEntity,
			float preferredDistance,
			float distanceTolerance)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<MovementTarget>(shipEntity))
			{
				return;
			}

			entityManager.SetComponentData(shipEntity, new MovementTarget
			{
				Reference = TargetReference.FromEntity(targetEntity),
				Mode = MovementTargetMode.DefendClose,
				PreferredDistance = preferredDistance,
				DistanceTolerance = distanceTolerance,
				ArrivalDistance = 0f
			});
		}

		public static bool TryResolveTargetPosition(
			EntityManager entityManager,
			TargetReference targetReference,
			out float2 worldPosition)
		{
			worldPosition = float2.zero;

			if (targetReference.Kind == TargetReferenceKind.None)
			{
				return false;
			}

			if (targetReference.Kind == TargetReferenceKind.WorldPosition)
			{
				worldPosition = targetReference.WorldPosition;
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

				if (!entityManager.HasComponent<CrossFire.Physics.WorldPose>(targetReference.Entity))
				{
					return false;
				}

				CrossFire.Physics.WorldPose worldPose =
					entityManager.GetComponentData<CrossFire.Physics.WorldPose>(targetReference.Entity);

				worldPosition = worldPose.Value.Position;
				return true;
			}

			return false;
		}
	}
}