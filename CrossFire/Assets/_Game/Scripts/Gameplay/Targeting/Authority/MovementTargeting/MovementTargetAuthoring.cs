using CrossFire.Core;
using Unity.Entities;
using UnityEngine;

namespace CrossFire.Ships
{
	public class MovementTargetAuthoring : MonoBehaviour
	{
		[Tooltip("Preferred distance to maintain from the target.")]
		public float PreferredDistance = 5f;
		[Tooltip("How far from PreferredDistance the ship is still considered in position.")]
		public float DistanceTolerance = 1f;
		[Tooltip("Distance at which a FlyToPoint target is considered reached.")]
		public float ArrivalDistance = 0.5f;

		class Baker : Baker<MovementTargetAuthoring>
		{
			public override void Bake(MovementTargetAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);

				AddComponent(entity, new MovementTarget
				{
					Reference = TargetReference.None(),
					Mode = MovementTargetMode.None,
					PreferredDistance = authoring.PreferredDistance,
					DistanceTolerance = authoring.DistanceTolerance,
					ArrivalDistance = authoring.ArrivalDistance
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
