using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace CrossFire.Core
{
	public static class CoreHelpers
	{
		public static void SetColor(EntityManager entityManager, Entity entity, float4 color)
		{
			entityManager.SetComponentData(entity, new URPMaterialPropertyBaseColor { Value = color });
		}
	}
}