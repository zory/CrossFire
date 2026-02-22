using Unity.Entities;
using UnityEngine;

public class BulletPrefabAuthoring : MonoBehaviour
{
	public float speed = 25f;
	public float lifetime = 2f;
	public float radius = 0.06f; // hit radius (world units)

	class Baker : Baker<BulletPrefabAuthoring>
	{
		public override void Bake(BulletPrefabAuthoring a)
		{
			var e = GetEntity(TransformUsageFlags.Dynamic);

			AddComponent<BulletTag>(e);
			AddComponent(e, new BulletVelocity { Value = 0 });
			AddComponent(e, new BulletLifetime { Seconds = a.lifetime });
			AddComponent(e, new BulletDamage { Value = 1 });

			// reuse ShipRadius as a generic radius for overlap tests
			AddComponent(e, new ShipRadius { Value = a.radius });

			// rendering comes from MeshRenderer/MeshFilter via Entities Graphics
		}
	}
}