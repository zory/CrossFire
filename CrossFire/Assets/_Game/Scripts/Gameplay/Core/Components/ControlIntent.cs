using Unity.Entities;

namespace CrossFire.Core
{
	public struct ControlIntent : IComponentData
	{
		public float Turn;    // -1..+1
		public float Thrust;  // -1..+1
		public byte Fire;     // 0/1 (bool in IComponentData is fine but byte is safer/clearer)
	}
}