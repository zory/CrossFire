using Unity.Entities;

namespace CrossFire.Core
{
	public struct CurrentTarget : IComponentData
	{
		public Entity Value;
	}
}