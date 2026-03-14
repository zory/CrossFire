using Unity.Entities;

namespace CrossFire.Ships
{
	[DisableAutoCreation]
	public partial class ShipsSpawnCommandBufferSystem : SystemBase
	{
		protected override void OnCreate()
		{
			var e = EntityManager.CreateEntity();
			EntityManager.AddComponent<SpawnShipsCommandBufferTag>(e);
			EntityManager.AddBuffer<SpawnShipsCommand>(e);
		}

		protected override void OnUpdate() { }
	}
}