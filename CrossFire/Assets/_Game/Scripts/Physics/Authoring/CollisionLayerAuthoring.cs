using Unity.Entities;
using UnityEngine;

namespace Core.Physics
{
	public class CollisionLayerAuthoring : MonoBehaviour
	{
		[Tooltip("One-hot bit flag identifying which layer this entity belongs to.")]
		public uint Layer = 1u;

		class Baker : Baker<CollisionLayerAuthoring>
		{
			public override void Bake(CollisionLayerAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new CollisionLayer { Value = authoring.Layer });
			}
		}
	}
}
