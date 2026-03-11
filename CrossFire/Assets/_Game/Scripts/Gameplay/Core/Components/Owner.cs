using Unity.Entities;

namespace CrossFire.Core
{
	public struct Owner : IComponentData
	{
		public Entity Value;
	}
}