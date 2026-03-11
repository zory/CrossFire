using Unity.Entities;

namespace CrossFire.Combat
{
	public struct ManualTarget : IComponentData
	{
		public Entity Value;
	}
}