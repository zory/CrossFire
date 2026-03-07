using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using CrossFire.Physics;

namespace CrossFire.Ships
{
	public class ShipPrefabAuthoring : MonoBehaviour
	{

		public float TurnSpeed = 3f;
		public float ThrustAcceleration = 5f;
		public float BrakeAcceleration = 5f;
		public short Health = 3;
		public float BulletLifetime = 2f;
		public float BulletSpeed = 10f;
		public float FireInterval = 0.5f;
		public float MuzzleOffset = 0.75f;

		class Baker : Baker<ShipPrefabAuthoring>
		{
			public override void Bake(ShipPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<TurnSpeed>(prefabEntity, new TurnSpeed() { Value = authoring.TurnSpeed });
				AddComponent<ThrustAcceleration>(prefabEntity, new ThrustAcceleration() { Value = authoring.ThrustAcceleration });
				AddComponent<BrakeAcceleleration>(prefabEntity, new BrakeAcceleleration() { Value = authoring.BrakeAcceleration });
				AddComponent<URPMaterialPropertyBaseColor>(prefabEntity);
				AddComponent<ShipTag>(prefabEntity);
				AddComponent<StableId>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<NativeColor>(prefabEntity);
				AddComponent<Health>(prefabEntity, new Health() { Value = authoring.Health, });
				AddComponent<SelectableTag>(prefabEntity);
				AddComponent<ShipIntent>(prefabEntity);
				AddComponent<Targetable>(prefabEntity);
				AddComponent<WeaponConfig>(prefabEntity, new WeaponConfig() { BulletLifetime = authoring.BulletLifetime, BulletSpeed = authoring.BulletSpeed, FireInterval = authoring.FireInterval, MuzzleOffset = authoring.MuzzleOffset });
				AddComponent<WeaponCooldown>(prefabEntity);
				AddComponent<BulletTargetTag>(prefabEntity);
				AddComponent<CollisionLayer>(prefabEntity, new CollisionLayer() { Value = 1 });
				AddComponent<CollisionMask>(prefabEntity, new CollisionMask() { Value = (1 << 0) | (1 << 1) });
			}
		}
	}
}