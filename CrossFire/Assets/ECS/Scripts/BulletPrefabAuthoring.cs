using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletPrefabAuthoring : MonoBehaviour
{
	public short Damage = 1;
	public float Lifetime = 2f;
	public short Health = 1;

	public float2 Size = new float2(1, 1);
	public float CollisionRadius = 0.06f; // hit radius (world units)

	class Baker : Baker<BulletPrefabAuthoring>
	{
		public override void Bake(BulletPrefabAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);

			AddComponent<BulletTag>(entity);

			AddComponent(entity, new TeamId { Value = 0 });

			AddComponent(entity, new Pos { Value = float2.zero });
			AddComponent(entity, new PrevPos { Value = float2.zero });
			AddComponent(entity, new Angle { Value = 0 });
			AddComponent(entity, new Velocity { Value = float2.zero });

			AddComponent(entity, new BulletLifetime { Seconds = authoring.Lifetime } );
			AddComponent(entity, new BulletDamage { Value = authoring.Damage } );

			AddComponent(entity, new Size { Value = authoring.Size });
			AddComponent(entity, new CollisionRadius { Value = authoring.CollisionRadius });
			AddComponent(entity, new Health { Value = authoring.Health });
		}
	}
}