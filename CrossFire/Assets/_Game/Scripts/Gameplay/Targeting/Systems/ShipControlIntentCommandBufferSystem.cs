using Unity.Entities;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	public partial struct ShipControlIntentCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<ShipControlIntentCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<ShipControlIntentCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}
}