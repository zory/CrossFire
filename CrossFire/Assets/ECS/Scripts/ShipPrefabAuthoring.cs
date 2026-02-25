using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Put this on the Quad that represents your ship visual INSIDE the SubScene.
// It becomes an Entity Prefab with render components baked by Entities Graphics.
public class ShipPrefabAuthoring : MonoBehaviour
{
	public float ThrustAcceleration = 25f;
	public float BrakeAcceleleration = 35f;
	public float MaxSpeed = 60f;
	public float TurnRateDeg = 180f;
	public short Health = 3;
	public float ShootCooldown;
	public float ShootSpeed;

	public float2 Size = new float2(1, 1);
	public float CollisionRadius = 0.25f;

	class Baker : Baker<ShipPrefabAuthoring>
	{
		public override void Bake(ShipPrefabAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);

			AddComponent<ShipTag>(entity);

			AddComponent(entity, new TeamId { Value = 0 });

			AddComponent(entity, new Pos { Value = float2.zero });
			AddComponent(entity, new PrevPos { Value = float2.zero });
			AddComponent(entity, new Angle { Value = 0 });
			AddComponent(entity, new Velocity { Value = float2.zero });

			AddComponent(entity, new Size { Value = authoring.Size });
			AddComponent(entity, new CollisionRadius { Value = authoring.CollisionRadius });
			AddComponent(entity, new Health { Value = authoring.Health });

			AddComponent(entity, new ThrustAcceleration { Value = authoring.ThrustAcceleration });
			AddComponent(entity, new BrakeAcceleleration { Value = authoring.BrakeAcceleleration });
			AddComponent(entity, new MaxSpeed { Value = authoring.MaxSpeed });
			AddComponent(entity, new TurnSpeed { Value = Mathf.Deg2Rad * authoring.TurnRateDeg });

			AddComponent(entity, new ShootCooldown { Value = authoring.ShootCooldown });
			AddComponent(entity, new ShootSpeed { Value = authoring.ShootSpeed });
		}
	}
}