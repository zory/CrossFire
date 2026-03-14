using Unity.Entities;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	public partial struct ClickPickRequestBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SelectionRequestBufferTag>(entity);
			state.EntityManager.AddBuffer<SelectionRequestCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}
}
