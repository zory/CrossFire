using Unity.Entities;
using UnityEngine;

namespace CrossFire.Physics
{
	public class LinearDampingAuthoring : MonoBehaviour
	{
		public float LinearDamping = 0f;

		class Baker : Baker<LinearDampingAuthoring>
		{
			public override void Bake(LinearDampingAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<LinearDamping>(prefabEntity, new LinearDamping() { Value = authoring.LinearDamping });
			}
		}
	}
}