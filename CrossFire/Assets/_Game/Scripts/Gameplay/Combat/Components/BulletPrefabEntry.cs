using Unity.Entities;

namespace CrossFire.Combat
{
	public struct BulletPrefabEntry : IBufferElementData
	{
		public BulletType Type;
		public Entity Prefab;
	}
}
