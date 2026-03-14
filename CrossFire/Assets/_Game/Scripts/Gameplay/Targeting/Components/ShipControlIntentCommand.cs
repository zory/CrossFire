using Unity.Entities;

namespace CrossFire.Targeting
{
	public struct ShipControlIntentCommand : IBufferElementData
	{
		public float Turn;
		public float Thrust;
		public bool Fire;

		public override string ToString()
		{
			return string.Format(
				"ShipControlIntentCommand. Turn:{0} Thrust:{1} Fire:{2}",
				Turn,
				Thrust,
				Fire
			);
		}
	}
}