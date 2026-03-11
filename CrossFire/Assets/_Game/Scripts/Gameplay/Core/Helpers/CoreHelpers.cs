using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Core
{
	public static class CoreHelpers
	{
		//Burst safe
		public static float4 GetTeamColor(DynamicBuffer<TeamColor> buffer, byte teamId)
		{
			if (teamId >= buffer.Length)
			{
				return new float4(1f, 1f, 1f, 1f);
			}

			return buffer[teamId].Value;
		}

		//Non Burst version
		public static float4 GetTeamColor(EntityManager entityManager, byte teamId)
		{
			using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TeamColor>());

			if (query.IsEmptyIgnoreFilter)
			{
				return new float4(1f, 1f, 1f, 1f);
			}

			Entity entity = query.GetSingletonEntity();
			DynamicBuffer<TeamColor> buffer = entityManager.GetBuffer<TeamColor>(entity);

			if (teamId >= buffer.Length)
			{
				return new float4(1f, 1f, 1f, 1f);
			}

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
