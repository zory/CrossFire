using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

namespace CrossFire.Ships
{
	public class WeaponAuthoring : MonoBehaviour
	{
		public BulletType BulletType = BulletType.SimpleBullet;
		public float BulletLifetime = 2f;
		public float BulletSpeed = 10f;
		public float FireInterval = 0.5f;
		public float MuzzleOffset = 0.75f;

		class Baker : Baker<WeaponAuthoring>
		{
			public override void Bake(WeaponAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<WeaponConfig>(prefabEntity, new WeaponConfig() { BulletType = authoring.BulletType, BulletLifetime = authoring.BulletLifetime, BulletSpeed = authoring.BulletSpeed, FireInterval = authoring.FireInterval, MuzzleOffset = authoring.MuzzleOffset });
				AddComponent<WeaponCooldown>(prefabEntity);
			}
		}
	}
}