using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public struct WeaponAimSolution : IBufferElementData
	{
		public byte WeaponSlotIndex;
		public Entity TrackedEntity;
		public float2 AimPoint;
		public float2 AimDirection;
		public float InterceptTime;
		public byte HasSolution;
	}
}
