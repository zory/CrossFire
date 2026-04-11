using Unity.Entities;
using UnityEngine;

namespace Core.Physics
{
	public class CollisionMaskAuthoring : MonoBehaviour
	{
		[Tooltip("Bitset of layers this entity can collide with. Must match bidirectionally with the other entity's CollisionLayer.")]
		public uint Mask = 0u;

		class Baker : Baker<CollisionMaskAuthoring>
		{
			public override void Bake(CollisionMaskAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new CollisionMask { Value = authoring.Mask });
			}
		}
	}
}
