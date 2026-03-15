using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	public struct SelectionRequestCommand : IBufferElementData
	{
		public float2 WorldPosition;
		public float PickRadius;

		public override string ToString()
		{
			return
				string.Format(
					"SelectionRequestCommand. " +
					"WorldPosition:{0} " +
					"PickRadius:{1}",
					WorldPosition, PickRadius
				);
		}
	}
}
