using Unity.Entities;
using UnityEngine;

// Put this on the Quad that represents your ship visual INSIDE the SubScene.
// It becomes an Entity Prefab with render components baked by Entities Graphics.
public class ShipPrefabAuthoring : MonoBehaviour
{
	[Header("Defaults (can be overridden by BattleConfig)")]
	public float shipRadius = 0.25f;
	public float shipSpeed = 5f;
	public float turnSpeedDegPerSec = 180f;

	class Baker : Baker<ShipPrefabAuthoring>
	{
		public override void Bake(ShipPrefabAuthoring authoring)
		{
			var entity = GetEntity(TransformUsageFlags.Dynamic);

			AddComponent<ShipTag>(entity);

			AddComponent(entity, new TeamId { Value = 0 });

			AddComponent(entity, new ShipPos { Value = 0 });
			AddComponent(entity, new ShipPrevPos { Value = 0 });
			AddComponent(entity, new ShipAngle { Value = 0 });

			AddComponent(entity, new ShipRadius { Value = authoring.shipRadius });
			AddComponent(entity, new ShipSpeed { Value = authoring.shipSpeed });
			AddComponent(entity, new ShipTurnSpeed { Value = Mathf.Deg2Rad * authoring.turnSpeedDegPerSec });

			// Rendering components are baked automatically from MeshRenderer/MeshFilter by Entities Graphics.
			// Do not add rendering components manually here.
		}
	}
}