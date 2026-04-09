using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Ships
{
	/// <summary>
	/// One-shot bootstrap that creates the singleton entity holding the
	/// <see cref="DynamicBuffer{T}"/> of <see cref="SpawnShipsCommand"/> requests.
	/// Disables itself after initialisation so <c>OnUpdate</c> is never called again.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Bootstrap — must run before <see cref="ShipsSpawnSystem"/> so the
	/// command buffer entity exists when the first spawn request is enqueued.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct ShipsSpawnCommandBufferSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SpawnShipsCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<SpawnShipsCommand>(entity);
			state.EntityManager.SetName(entity, new FixedString64Bytes("SpawnCommandBuffer"));
			state.Enabled = false;
		}

		public void OnUpdate(ref SystemState state) { }
	}
}
