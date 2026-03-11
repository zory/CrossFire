using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Core
{
	public static class CoreHelpers
	{
		public static float4 GetTeamColor(EntityManager entityManager, byte teamId)
		{
			using var query = entityManager.CreateEntityQuery(typeof(TeamColor));

			if (query.IsEmpty)
			{
				return new float4(255, 255, 255, 255);
			}

			var entity = query.GetSingletonEntity();
			var buffer = entityManager.GetBuffer<TeamColor>(entity);

			return buffer[teamId].Value;
		}

		public static void SetColor(EntityManager entityManager, Entity entity, float4 color)
		{
			//Just created or not existing
			if (!entityManager.Exists(entity))
			{
				return;
			}

			//Tries setting for this entity
			if (entityManager.HasComponent<MaterialMeshInfo>(entity) && entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
			{
				entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor
				{
					Value = color
				});
			}

			//Tries settings for children entities
			if (!entityManager.HasBuffer<LinkedEntityGroup>(entity))
			{
				return;
			}

			DynamicBuffer<LinkedEntityGroup> linked = entityManager.GetBuffer<LinkedEntityGroup>(entity);
			int linkedLength = linked.Length;
			for (int i = 0; i < linkedLength; i++)
			{
				Entity child = linked[i].Value;

				//Just created or not existing
				if (!entityManager.Exists(child))
				{
					continue;
				}

				//No mesh
				if (!entityManager.HasComponent<MaterialMeshInfo>(child))
				{
					continue;
				}

				//Not marked for color
				if (!entityManager.HasComponent<URPMaterialPropertyBaseColor>(child))
				{
					continue;
				}

				//Set color
				entityManager.SetComponentData(child, new URPMaterialPropertyBaseColor
				{
					Value = color
				});
			}
		}
	}
}
