using Unity.Entities;

namespace CrossFire.Core
{
	public struct MovementTarget : IComponentData
	{
		public TargetReference Reference;
		public MovementTargetMode Mode;

		// Used by range-based modes.
		public float PreferredDistance;
		public float DistanceTolerance;

		// Used by FlyToPoint.
		public float ArrivalDistance;
	}
}