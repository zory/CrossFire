using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	//[BurstCompile]
	public partial struct TargetAcquireSystem : ISystem
	{
		private EntityQuery _candidateQuery;

		public void OnCreate(ref SystemState state)
		{
			_candidateQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					ComponentType.ReadOnly<WorldPose>(),
					ComponentType.ReadOnly<TeamId>(),
					ComponentType.ReadOnly<TargetableTag>(),
				}
			});

			state.RequireForUpdate<NeedsTargetTag>();
			state.RequireForUpdate(_candidateQuery);
		}

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			using NativeArray<Entity> candidateEntities = _candidateQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<WorldPose> candidatePoses = _candidateQuery.ToComponentDataArray<WorldPose>(Allocator.Temp);
			using NativeArray<TeamId> candidateTeams = _candidateQuery.ToComponentDataArray<TeamId>(Allocator.Temp);

			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach ((RefRO<WorldPose> selfPose, RefRO<TeamId> selfTeam, RefRO<TargetingProfile> targetingProfile, RefRW<CurrentTarget> currentTarget, Entity selfEntity) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<TeamId>, RefRO<TargetingProfile>, RefRW<CurrentTarget>>()
						.WithAll<NeedsTargetTag>()
						.WithEntityAccess())
			{
				if (targetingProfile.ValueRO.Mode == TargetingMode.Manual)
				{
					continue;
				}

				Entity bestTarget = Entity.Null;

				if (targetingProfile.ValueRO.Mode == TargetingMode.StickyNearest)
				{
					bestTarget = FindNearestEnemy(
						entityManager,
						selfEntity,
						selfTeam.ValueRO.Value,
						selfPose.ValueRO.Value.Position,
						candidateEntities,
						candidatePoses,
						candidateTeams
					);
				}
				else if (targetingProfile.ValueRO.Mode == TargetingMode.ThreatRetarget)
				{
					bestTarget = FindThreatTarget(
						entityManager,
						selfEntity,
						selfTeam.ValueRO.Value,
						selfPose.ValueRO.Value.Position,
						candidateEntities,
						candidatePoses,
						candidateTeams
					);
				}

				currentTarget.ValueRW.Value = bestTarget;

				if (bestTarget != Entity.Null)
				{
					entityCommandBuffer.RemoveComponent<NeedsTargetTag>(selfEntity);
				}
			}

			entityCommandBuffer.Playback(entityManager);
			entityCommandBuffer.Dispose();
		}

		//[BurstCompile]
		private static Entity FindNearestEnemy(
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

				if (!IsCandidateValid(entityManager, selfEntity, selfTeamId, candidateEntity, candidateTeams[index].Value))
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

		//[BurstCompile]
		private static Entity FindThreatTarget(
			EntityManager entityManager,
			Entity selfEntity,
			byte selfTeamId,
			float2 selfPosition,
			NativeArray<Entity> candidateEntities,
			NativeArray<WorldPose> candidatePoses,
			NativeArray<TeamId> candidateTeams)
		{
			Entity bestEntity = Entity.Null;
			float bestScore = float.MinValue;

			for (int index = 0; index < candidateEntities.Length; index++)
			{
				Entity candidateEntity = candidateEntities[index];

				if (!IsCandidateValid(entityManager, selfEntity, selfTeamId, candidateEntity, candidateTeams[index].Value))
				{
					continue;
				}

				float2 delta = candidatePoses[index].Value.Position - selfPosition;
				float distanceSq = math.dot(delta, delta);

				float score = 1f / (distanceSq + 1f);

				if (score > bestScore)
				{
					bestScore = score;
					bestEntity = candidateEntity;
				}
			}

			return bestEntity;
		}

		//[BurstCompile]
		private static bool IsCandidateValid(
			EntityManager entityManager,
			Entity selfEntity,
			byte selfTeamId,
			Entity candidateEntity,
			byte candidateTeamId)
		{
			if (candidateEntity == Entity.Null)
			{
				return false;
			}

			if (candidateEntity == selfEntity)
			{
				return false;
			}

			if (!entityManager.Exists(candidateEntity))
			{
				return false;
			}

			if (candidateTeamId == selfTeamId)
			{
				return false;
			}

			return true;
		}
	}
}