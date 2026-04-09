using CrossFire.Core;
using Core.Physics;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	/// <summary>
	/// Authoring component for ship prefabs.
	/// Configure movement and health values here; the baker converts them into the
	/// corresponding ECS components used by <see cref="ShipMovementSystem"/> and the
	/// combat systems.
	/// </summary>
	public class ShipPrefabAuthoring : MonoBehaviour
	{
		public float TurnSpeed = 3f;
		public float ThrustAcceleration = 5f;
		public float BrakeAcceleration = 5f;
		public short Health = 3;

		class ShipPrefabBaker : Baker<ShipPrefabAuthoring>
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

				//Dynamic movement
				AddComponent<TurnSpeed>(prefabEntity, new TurnSpeed() { Value = authoring.TurnSpeed });
				AddComponent<ThrustAcceleration>(prefabEntity, new ThrustAcceleration() { Value = authoring.ThrustAcceleration });
				AddComponent<BrakeAcceleration>(prefabEntity, new BrakeAcceleration() { Value = authoring.BrakeAcceleration });

				//Control
				AddComponent<SelectableTag>(prefabEntity);
				AddComponent<ControlIntent>(prefabEntity);

				//Collision
				AddComponent<CollisionLayer>(prefabEntity, new CollisionLayer() { Value = 1 });
				AddComponent<CollisionMask>(prefabEntity, new CollisionMask() { Value = (1 << 0) | (1 << 1) });
			}
		}
	}
}