using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	public class MovementTargetAuthoring : MonoBehaviour
	{
		class Baker : Baker<MovementTargetAuthoring>
		{
			public override void Bake(MovementTargetAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent(entity, new MovementTarget
				{
					Reference = TargetReference.None(),
					Mode = MovementTargetMode.None,
					PreferredDistance = 0f,
					DistanceTolerance = 0f,
					ArrivalDistance = 0f
				});

				AddComponent(entity, new MovementTargetResolved
				{
					WorldPosition = default,
					HasTarget = 0
				});
			}
		}
	}
}