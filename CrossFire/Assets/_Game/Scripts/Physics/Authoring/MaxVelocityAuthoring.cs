using Unity.Entities;
using UnityEngine;

namespace CrossFire.Physics
{
	public class MaxVelocityAuthoring : MonoBehaviour
	{
		public float MaxVelocity = 5f;

		class Baker : Baker<MaxVelocityAuthoring>
		{
			public override void Bake(MaxVelocityAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<MaxVelocity>(prefabEntity, new MaxVelocity() { Value = authoring.MaxVelocity });
			}
		}
	}
}