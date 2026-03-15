using Unity.Entities;

namespace CrossFire.Ships
{
	[DisableAutoCreation]
	public partial struct ShipsSpawnCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{  
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SpawnShipsCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<SpawnShipsCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}
}