using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace CrossFire.Presentation
{
	public class TintableAuthoring : MonoBehaviour
	{
		public Color InitialColor = Color.white;

		class Baker : Baker<TintableAuthoring>
		{
			public override void Bake(TintableAuthoring authoring)
			{
				var entity = GetEntity(TransformUsageFlags.Renderable);

				AddComponent(entity, new URPMaterialPropertyBaseColor
				{
					Value = new float4(
						authoring.InitialColor.r,
						authoring.InitialColor.g,
						authoring.InitialColor.b,
						authoring.InitialColor.a)
				});
			}
		}
	}
}