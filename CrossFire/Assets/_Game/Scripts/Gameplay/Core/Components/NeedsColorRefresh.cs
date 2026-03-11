using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public struct NeedsColorRefresh : IComponentData
	{
		public float4 Value;
	}
}