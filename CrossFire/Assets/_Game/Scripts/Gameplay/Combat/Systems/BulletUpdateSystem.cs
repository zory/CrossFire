using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace CrossFire.Combat
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct BulletUpdateSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			float deltaTime = SystemAPI.Time.DeltaTime;
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRW<Lifetime> lifetime, Entity entity) in
					 SystemAPI.Query<RefRW<Lifetime>>()
						.WithAll<BulletTag>()
						.WithEntityAccess())
			{
				float timeLeft = lifetime.ValueRO.TimeLeft - deltaTime;
				lifetime.ValueRW.TimeLeft = timeLeft;

				if (timeLeft <= 0f)
				{
					entityCommandBuffer.DestroyEntity(entity);
				}
			}

			entityCommandBuffer.Playback(state.EntityManager);
			entityCommandBuffer.Dispose();
		}
	}
}