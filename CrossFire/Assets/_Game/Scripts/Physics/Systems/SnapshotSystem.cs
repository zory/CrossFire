using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Core.Physics
{
	/// <summary>
	/// Copies <see cref="WorldPose"/> into <see cref="PrevWorldPose"/> at the start of
	/// each simulation tick, giving downstream systems a stable reference to where each
	/// body was at the beginning of the frame.
	/// </summary>
	/// <remarks>
	/// Pipeline phase: Snapshot — must run first in the simulation, before any intent,
	/// movement, or physics systems. In a new application, register it immediately after
	/// any bootstrap / initialisation systems and before spawn or intent systems.
	/// </remarks>
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct SnapshotSystem : ISystem
	{
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldPose, PrevWorldPose>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			foreach (var (pose, prevPose) in SystemAPI.Query<RefRO<WorldPose>, RefRW<PrevWorldPose>>())
			{
				prevPose.ValueRW.Value = pose.ValueRO.Value;
			}
		}
	}
}
