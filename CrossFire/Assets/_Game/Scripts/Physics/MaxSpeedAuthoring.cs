using Unity.Entities;
using UnityEngine;

namespace CrossFire.Physics
{
	public class MaxSpeedAuthoring : MonoBehaviour
	{
		public float MaxSpeed = 5f;

		class Baker : Baker<MaxSpeedAuthoring>
		{
			public override void Bake(MaxSpeedAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<MaxVelocity>(prefabEntity, new MaxVelocity() { Value = authoring.MaxSpeed });
			}
		}
	}
}