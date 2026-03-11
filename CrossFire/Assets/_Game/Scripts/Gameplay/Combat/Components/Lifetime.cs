using Unity.Entities;

namespace CrossFire.Combat
{
	public struct Lifetime : IComponentData
	{
		public float TimeLeft;
	}
}