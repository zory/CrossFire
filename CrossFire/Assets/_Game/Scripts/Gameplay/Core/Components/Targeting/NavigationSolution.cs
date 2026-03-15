using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Core
{
	public struct NavigationSolution : IComponentData
	{
		public float2 Destination;
		public byte HasSolution;
	}
}
