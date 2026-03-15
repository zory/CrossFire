using Unity.Entities;

namespace CrossFire.Targeting
{
	public struct ShipControlIntentCommand : IBufferElementData
	{
		public float Turn;   // -1..+1
		public float Thrust; // -1..+1
		public bool Fire;

		public override string ToString()
		{
			return
				string.Format(
					"ShipMoveCommand. " +
					"Turn:{0} " +
					"Thrust:{1} " +
					"Fire:{2} ",
					Turn, Thrust, Fire
				);
		}
	}
}
