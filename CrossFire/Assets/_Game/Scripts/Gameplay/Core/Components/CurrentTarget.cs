using Unity.Entities;

namespace CrossFire.Combat
{
	public struct CurrentTarget : IComponentData
	{
		public Entity Value;
	}
}