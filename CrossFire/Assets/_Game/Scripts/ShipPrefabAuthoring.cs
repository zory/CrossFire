using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CrossFire.Ships
{
	public class ShipPrefabAuthoring : MonoBehaviour
	{
		public float CollisionRadius = 0.5f;
		public float MaxSpeed = 5f;
		public float TurnSpeed = 3f;
		public float ThrustAcceleration = 5f;
		public float BrakeAcceleration = 5f;

		class Baker : Baker<ShipPrefabAuthoring>
		{
			public override void Bake(ShipPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<PrevWorldPose>(prefabEntity);
				AddComponent<WorldPose>(prefabEntity);
				AddComponent<LocalTransform>(prefabEntity);
				AddComponent<CollisionRadius>(prefabEntity, new CollisionRadius() { Value = authoring.CollisionRadius });
				AddComponent<Velocity>(prefabEntity);
				AddComponent<MaxSpeed>(prefabEntity, new MaxSpeed() { Value = authoring.MaxSpeed });
				AddComponent<TurnSpeed>(prefabEntity, new TurnSpeed() { Value = authoring.TurnSpeed });
				AddComponent<ThrustAcceleration>(prefabEntity, new ThrustAcceleration() { Value = authoring.ThrustAcceleration });
				AddComponent<BrakeAcceleleration>(prefabEntity, new BrakeAcceleleration() { Value = authoring.BrakeAcceleration });
				AddComponent<URPMaterialPropertyBaseColor>(prefabEntity);
				AddComponent<ShipTag>(prefabEntity);
				AddComponent<StableId>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);
				AddComponent<NativeColor>(prefabEntity);
				AddComponent<ShootCooldown>(prefabEntity);
				AddComponent<ShootSpeed>(prefabEntity);
				AddComponent<Health>(prefabEntity);
				AddComponent<SelectableTag>(prefabEntity);
				AddComponent<ShipIntent>(prefabEntity);
				AddComponent<Targetable>(prefabEntity);
			}
		}
	}
}