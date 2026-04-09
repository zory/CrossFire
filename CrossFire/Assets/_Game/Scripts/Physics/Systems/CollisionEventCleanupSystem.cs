using Unity.Burst;
using Unity.Entities;

namespace Core.Physics
{
	/// <summary>
	/// Clears the singleton <see cref="CollisionEventBufferTag"/> buffer at the end of each
	/// frame, after all combat-reaction systems have finished reading collision events.
	/// This guarantees events never persist into the next frame even if
	/// <see cref="CollisionDetectionSystem"/> does not run.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Cleanup — runs after all systems that consume
	/// <see cref="CollisionEvent"/> entries (bullet damage, death, etc.) and before the
	/// next frame's detection pass.
	/// <see cref="CollisionDetectionSystem"/> also clears the buffer at the start of its own
	/// update; this system is the safety net for frames where detection is skipped.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct CollisionEventCleanupSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CollisionEventBufferTag>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			Entity collisionEventBufferEntity = SystemAPI.GetSingletonEntity<CollisionEventBufferTag>();
			DynamicBuffer<CollisionEvent> collisionEventBuffer = state.EntityManager.GetBuffer<CollisionEvent>(collisionEventBufferEntity);
			collisionEventBuffer.Clear();
		}
	}
}
