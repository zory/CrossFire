using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Core
{
	public static class CoreHelpers
	{
		public static float4 GetTeamColor(DynamicBuffer<TeamColor> buffer, byte teamId)
		{
			if (teamId >= buffer.Length)
			{
				return new float4(1f, 1f, 1f, 1f);
			}

			return buffer[teamId].Value;
		}

		public static void SetColor(EntityManager entityManager, Entity entity, float4 color)
		{
			if (!entityManager.Exists(entity))
			{
				return;
			}

			if (entityManager.HasComponent<MaterialMeshInfo>(entity) &&
				entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
			{
				entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor
				{
					Value = color
				});
			}

			if (!entityManager.HasBuffer<LinkedEntityGroup>(entity))
			{
				return;
			}

			DynamicBuffer<LinkedEntityGroup> linked = entityManager.GetBuffer<LinkedEntityGroup>(entity);

			for (int i = 0; i < linked.Length; i++)
			{
				Entity child = linked[i].Value;

				if (!entityManager.Exists(child))
				{
					continue;
				}

				if (!entityManager.HasComponent<MaterialMeshInfo>(child))
				{
					continue;
				}

				if (!entityManager.HasComponent<URPMaterialPropertyBaseColor>(child))
				{
					continue;
				}

				entityManager.SetComponentData(child, new URPMaterialPropertyBaseColor
				{
					Value = color
				});
			}
		}
	}
}