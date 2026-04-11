using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Core
{
	// NativeColor is overwritten at spawn time by ShipsSpawnSystem with the
	// team colour; the value set here is only visible before the first spawn.
	public class NativeColorAuthoring : MonoBehaviour
	{
		public Color InitialColor = Color.white;

		class Baker : Baker<NativeColorAuthoring>
		{
			public override void Bake(NativeColorAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				Color c = authoring.InitialColor;
				AddComponent(entity, new NativeColor { Value = new float4(c.r, c.g, c.b, c.a) });
			}
		}
	}
}
