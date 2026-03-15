using Unity.Entities;

namespace CrossFire.Core
{
	public struct NavigationTarget : IComponentData
	{
		public TargetReference Value;
	}
}