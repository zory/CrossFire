using Core.Physics;
using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// A single deferred ship-spawn request written into the
	/// <see cref="SpawnShipsCommandBufferTag"/> buffer entity.
	/// Processed and cleared each frame by <see cref="ShipsSpawnSystem"/>.
	/// </summary>
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