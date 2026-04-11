using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	/// <summary>
	/// Authoring component for ship prefabs.
	/// Adds only the components that are unique to ships: <see cref="ShipTag"/>,
	/// <see cref="TurnSpeed"/>, <see cref="ThrustAcceleration"/>, and
	/// <see cref="BrakeAcceleration"/>.
	///
	/// All other components (physics body, collider, health, team, colour, weapon,
	/// control intent, targeting, etc.) are contributed by their own dedicated
	/// authoring components sitting alongside this one on the prefab GameObject.
	/// </summary>
	public class ShipPrefabAuthoring : MonoBehaviour
	{
		public float TurnSpeed = 3f;
		public float ThrustAcceleration = 5f;
		public float BrakeAcceleration = 5f;

		class ShipPrefabBaker : Baker<ShipPrefabAuthoring>
		{
			public override void Bake(ShipPrefabAuthoring authoring)
			{
				Entity prefabEntity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent<ShipTag>(prefabEntity);
				AddComponent(prefabEntity, new TurnSpeed { Value = authoring.TurnSpeed });
				AddComponent(prefabEntity, new ThrustAcceleration { Value = authoring.ThrustAcceleration });
				AddComponent(prefabEntity, new BrakeAcceleration { Value = authoring.BrakeAcceleration });
			}
		}
	}
}
