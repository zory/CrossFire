using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Physics;
using CrossFire.Player;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace CrossFire.Ships
{
	public class ShipPrefabAuthoring : MonoBehaviour
	{
		public float TurnSpeed = 3f;
		public float ThrustAcceleration = 5f;
		public float BrakeAcceleration = 5f;
		public short Health = 3;

		[Header("Targeting")]
		public TargetingMode TargetingMode = TargetingMode.StickyNearest;
		public float RetargetInterval = 2f;

		class Baker : Baker<ShipPrefabAuthoring>
		{
			public override void Bake(ShipPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<ShipTag>(prefabEntity);

				//Common
				AddComponent<StableId>(prefabEntity);
				AddComponent<TeamId>(prefabEntity);

				//Rendering
				AddComponent<NativeColor>(prefabEntity);



				AddComponent<Health>(prefabEntity, new Health() { Value = authoring.Health, });

				AddComponent<TurnSpeed>(prefabEntity, new TurnSpeed() { Value = authoring.TurnSpeed });
				AddComponent<ThrustAcceleration>(prefabEntity, new ThrustAcceleration() { Value = authoring.ThrustAcceleration });
				AddComponent<BrakeAcceleration>(prefabEntity, new BrakeAcceleration() { Value = authoring.BrakeAcceleration });

				AddComponent<SelectableTag>(prefabEntity);
				AddComponent<ControlIntent>(prefabEntity);

				//Collision
				AddComponent<CollisionLayer>(prefabEntity, new CollisionLayer() { Value = 1 });
				AddComponent<CollisionMask>(prefabEntity, new CollisionMask() { Value = (1 << 0) | (1 << 1) });

				//Targeting
				AddComponent<TargetableTag>(prefabEntity);
				AddComponent<CurrentTarget>(prefabEntity, new CurrentTarget() { Value = Entity.Null });
				AddComponent<TargetingProfile>(prefabEntity, new TargetingProfile()
				{
					Mode = authoring.TargetingMode,
					RetargetInterval = authoring.RetargetInterval,
				});
				AddComponent<TargetRetargetTimer>(prefabEntity, new TargetRetargetTimer()
				{
					TimeLeft = authoring.RetargetInterval,
				});
				AddComponent<ManualTarget>(prefabEntity, new ManualTarget()
				{
					Value = Entity.Null,
				});
			}
		}
	}
}