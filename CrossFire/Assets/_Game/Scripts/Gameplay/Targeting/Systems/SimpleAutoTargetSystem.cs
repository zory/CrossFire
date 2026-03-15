using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct SimpleAutoTargetSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<NavigationTarget>();
			state.RequireForUpdate<TeamId>();
			state.RequireForUpdate<WorldPose>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			NativeArray<Entity> candidateEntities = SystemAPI.QueryBuilder()
				.WithAll<WorldPose, TeamId, TargetableTag>()
				.Build()
				.ToEntityArray(Allocator.Temp);

			NativeArray<WorldPose> candidatePoses = SystemAPI.QueryBuilder()
				.WithAll<WorldPose, TeamId, TargetableTag>()
				.Build()
				.ToComponentDataArray<WorldPose>(Allocator.Temp);

			NativeArray<TeamId> candidateTeams = SystemAPI.QueryBuilder()
				.WithAll<WorldPose, TeamId, TargetableTag>()
				.Build()
				.ToComponentDataArray<TeamId>(Allocator.Temp);

			foreach ((RefRO<WorldPose> selfPose,
					  RefRO<TeamId> selfTeam,
					  RefRW<NavigationTarget> navigationTarget,
					  DynamicBuffer<WeaponTarget> weaponTargets,
					  Entity selfEntity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<TeamId>, RefRW<NavigationTarget>, DynamicBuffer<WeaponTarget>>()
						.WithNone<ControlledTag>()
						.WithEntityAccess())
			{
				Entity bestEntity = FindClosestEnemy(
					entityManager,
					selfEntity,
					selfTeam.ValueRO.Value,
					selfPose.ValueRO.Value.Position,
					candidateEntities,
					candidatePoses,
					candidateTeams);

				if (bestEntity == Entity.Null)
				{
					navigationTarget.ValueRW.Value = TargetReference.None();
					weaponTargets.Clear();
					continue;
				}

				navigationTarget.ValueRW.Value = TargetReference.FromEntity(bestEntity);

				weaponTargets.Clear();
				weaponTargets.Add(new WeaponTarget
				{
					WeaponSlotIndex = 0,
					Behavior = WeaponTargetingBehavior.DirectFire,
					Target = TargetReference.FromEntity(bestEntity)
				});
			}

			candidateEntities.Dispose();
			candidatePoses.Dispose();
			candidateTeams.Dispose();
		}

		[BurstCompile]
		private static Entity FindClosestEnemy(
			EntityManager entityManager,
			Entity selfEntity,
			byte selfTeamId,
			float2 selfPosition,
			NativeArray<Entity> candidateEntities,
			NativeArray<WorldPose> candidatePoses,
			NativeArray<TeamId> candidateTeams)
		{
			Entity bestEntity = Entity.Null;
			float bestDistanceSq = float.MaxValue;

			for (int index = 0; index < candidateEntities.Length; index++)
			{
				Entity candidateEntity = candidateEntities[index];

				if (candidateEntity == Entity.Null)
				{
					continue;
				}

				if (candidateEntity == selfEntity)
				{
					continue;
				}

				if (!entityManager.Exists(candidateEntity))
				{
					continue;
				}

				if (candidateTeams[index].Value == selfTeamId)
				{
					continue;
				}

				float2 delta = candidatePoses[index].Value.Position - selfPosition;
				float distanceSq = math.dot(delta, delta);

				if (distanceSq < bestDistanceSq)
				{
					bestDistanceSq = distanceSq;
					bestEntity = candidateEntity;
				}
			}

			return bestEntity;
		}
	}
}