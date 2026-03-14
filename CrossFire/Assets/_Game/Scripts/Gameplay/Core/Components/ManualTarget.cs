using Unity.Entities;

namespace CrossFire.Core
{
	public struct ManualTarget : IComponentData
	{
		public Entity Value;
	}
}