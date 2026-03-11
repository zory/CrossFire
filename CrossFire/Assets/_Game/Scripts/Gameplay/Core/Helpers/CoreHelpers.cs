using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Core
{
	public static class CoreHelpers
	{
		public static float4 GetTeamColor(EntityManager em, int team)
		{
			using var query = em.CreateEntityQuery(typeof(TeamColor));

			if (query.IsEmpty)
				return new float4(1, 1, 1, 1);

			var entity = query.GetSingletonEntity();
			var buffer = em.GetBuffer<TeamColor>(entity);

			if (team < 0 || team >= buffer.Length)
				return new float4(1, 1, 1, 1);

			return buffer[team].Value;
		}

		public static void SetColor(EntityManager entityManager, Entity entity, float4 color)
		{
			//Tries setting for this entity
			if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
			{
				entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor { Value = color });
			}

			if (entityManager.HasBuffer<LinkedEntityGroup>(entity))
			{
				var linked = entityManager.GetBuffer<LinkedEntityGroup>(entity);
				var linkedLength = linked.Length;

				for (int i = 0; i < linkedLength; i++)
				{
					var child = linked[i].Value;

					if (!entityManager.HasComponent<MaterialMeshInfo>(child))
						continue;

					if (!entityManager.HasComponent<URPMaterialPropertyBaseColor>(child))
					{
						entityManager.AddComponentData(child,
							new URPMaterialPropertyBaseColor
							{
								Value = color
							});
					}
					else
					{
						entityManager.SetComponentData(child,
							new URPMaterialPropertyBaseColor
							{
								Value = color
							});
					}
				}
			}

		}
	}
}
