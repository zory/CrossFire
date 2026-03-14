using CrossFire.Physics;
using Unity.Entities;

namespace CrossFire.Ships
{
	public struct SpawnShipsCommand : IBufferElementData
	{
		public int Id;
		public ShipType Type;
		public byte Team;
		public Pose2D Pose;

		public override string ToString()
		{
			return
				string.Format(
					"SpawnShipsCommand. " +
					"Id:{0} " +
					"Type:{1} " +
					"Team:{2} " +
					"Pose:{3}",
					Id, Type, Team, Pose
				);
		}
	}
}