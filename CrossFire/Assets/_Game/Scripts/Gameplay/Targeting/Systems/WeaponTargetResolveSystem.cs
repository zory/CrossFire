using CrossFire.Core;
using CrossFire.Physics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct WeaponTargetResolveSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<WeaponTarget>();
			state.RequireForUpdate<WorldPose>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			foreach ((RefRO<WorldPose> selfPose, DynamicBuffer<WeaponTarget> weaponTargets, DynamicBuffer<WeaponAimSolution> weaponAimSolutions) in
					 SystemAPI.Query<RefRO<WorldPose>, DynamicBuffer<WeaponTarget>, DynamicBuffer<WeaponAimSolution>>())
			{
				weaponAimSolutions.Clear();

				float2 selfPosition = selfPose.ValueRO.Value.Position;
				float selfTheta = selfPose.ValueRO.Value.ThetaRad;
				float2 selfForward = new float2(-math.sin(selfTheta), math.cos(selfTheta));

				for (int index = 0; index < weaponTargets.Length; index++)
				{
					WeaponTarget weaponTarget = weaponTargets[index];

					WeaponAimSolution solution = new WeaponAimSolution
					{
						WeaponSlotIndex = weaponTarget.WeaponSlotIndex,
						TrackedEntity = Entity.Null,
						AimPoint = selfPosition,
						AimDirection = selfForward,
						InterceptTime = 0f,
						HasSolution = 0
					};

					if (weaponTarget.Behavior == WeaponTargetingBehavior.FixedForward)
					{
						solution.AimPoint = selfPosition + selfForward;
						solution.AimDirection = selfForward;
						solution.HasSolution = 1;
						weaponAimSolutions.Add(solution);
						continue;
					}

					if (!TargetingHelpers.TryResolveTargetPosition(entityManager, weaponTarget.Target, out float2 targetPosition))
					{
						weaponAimSolutions.Add(solution);
						continue;
					}

					float2 aimDirection = math.normalizesafe(targetPosition - selfPosition, selfForward);

					solution.AimPoint = targetPosition;
					solution.AimDirection = aimDirection;
					solution.HasSolution = 1;

					if (weaponTarget.Behavior == WeaponTargetingBehavior.LockOnTrack &&
						weaponTarget.Target.Kind == TargetReferenceKind.Entity)
					{
						solution.TrackedEntity = weaponTarget.Target.Entity;
					}

					// LeadFire currently falls back to direct aim because no velocity component
					// was available in the uploaded files to compute prediction safely.

					weaponAimSolutions.Add(solution);
				}
			}
		}
	}
}