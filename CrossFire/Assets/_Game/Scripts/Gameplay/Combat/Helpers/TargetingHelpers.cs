using CrossFire.Core;
using Unity.Entities;

namespace CrossFire.Combat
{
	public static class TargetingHelpers
	{
		public static void SetManualTarget(EntityManager entityManager, Entity shipEntity, Entity targetEntity)
		{
			if (!entityManager.Exists(shipEntity))
			{
				return;
			}

			if (!entityManager.HasComponent<TargetingProfile>(shipEntity))
			{
				return;
			}

			TargetingProfile targetingProfile = entityManager.GetComponentData<TargetingProfile>(shipEntity);
			if (targetingProfile.Mode != TargetingMode.Manual)
			{
				return;
			}

			if (entityManager.HasComponent<ManualTarget>(shipEntity))
			{
				entityManager.SetComponentData(shipEntity, new ManualTarget()
				{
					Value = targetEntity,
				});
			}

			if (!entityManager.HasComponent<NeedsTargetTag>(shipEntity))
			{
				entityManager.AddComponent<NeedsTargetTag>(shipEntity);
			}
		}
	}
}