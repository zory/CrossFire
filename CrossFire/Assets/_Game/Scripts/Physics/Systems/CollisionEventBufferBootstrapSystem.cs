using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Core.Physics
{
	/// <summary>
	/// One-shot initializer that creates the singleton <see cref="CollisionEventBufferTag"/>
	/// entity and its <see cref="CollisionEvent"/> buffer the first time the world is set up.
	/// If the singleton already exists (e.g. created by a subscene or another bootstrap) the
	/// system leaves it untouched.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Bootstrap — runs before any system that reads or writes
	/// <see cref="CollisionEvent"/> entries.
	/// The system disables itself after <see cref="OnCreate"/> completes so that
	/// <see cref="OnUpdate"/> is never called and incurs no per-frame cost.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct CollisionEventBufferBootstrapSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			EntityQuery collisionEventBufferQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<CollisionEventBufferTag>()
				.Build(ref state);

			if (!collisionEventBufferQuery.IsEmptyIgnoreFilter)
			{
				state.Enabled = false;
				return;
			}

			Entity eventBufferEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<CollisionEventBufferTag>(eventBufferEntity);
			state.EntityManager.AddBuffer<CollisionEvent>(eventBufferEntity);
			state.EntityManager.SetName(eventBufferEntity, new FixedString64Bytes("CollisionEventBuffer"));

			state.Enabled = false;
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
		}
	}
}
