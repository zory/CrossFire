using Unity.Entities;
using UnityEngine;

namespace Core.Physics
{
	public class CollisionGridSettingsAuthoring : MonoBehaviour
	{
		public float CellSize = 4f;
	}

	public class CollisionGridSettingsBaker : Baker<CollisionGridSettingsAuthoring>
	{
		public override void Bake(CollisionGridSettingsAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new CollisionGridSettings
			{
				CellSize = authoring.CellSize
			});
		}
	}
}