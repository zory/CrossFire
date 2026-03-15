using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public struct MovementTargetResolved : IComponentData
	{
		public float2 WorldPosition;
		public byte HasTarget;
	}
}