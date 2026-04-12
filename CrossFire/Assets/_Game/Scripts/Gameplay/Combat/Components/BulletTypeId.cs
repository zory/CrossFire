using Unity.Entities;

namespace CrossFire.Combat
{
	/// <summary>
	/// Records the <see cref="BulletType"/> used to instantiate this bullet entity.
	/// Set by <see cref="WeaponFireSystem"/> at fire time so the type is available
	/// for serialization without consulting the prefab registry.
	/// </summary>
	public struct BulletTypeId : IComponentData
	{
		public BulletType Value;
	}
}
