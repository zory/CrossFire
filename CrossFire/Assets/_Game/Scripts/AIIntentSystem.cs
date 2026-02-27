using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace CrossFire.Ships
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(PlayerIntentSystem))]
	[BurstCompile]
	public partial struct AIIntentSystem : ISystem
	{
		// Tuneables (could be components/singleton later)
		private const float FireRange = 10f;
		private const float FireConeCos = 0.98f; // ~11.5 degrees

		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Targetable>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			foreach (var (pose, target, intent) in
					 SystemAPI.Query<RefRO<WorldPose>, RefRO<Targetable>, RefRW<ShipIntent>>()
							  .WithNone<ControlledTag>())
			{
				Entity t = target.ValueRO.Value;

				// Default: no intent if no target
				if (t == Entity.Null || !em.Exists(t) || !em.HasComponent<WorldPose>(t))
				{
					intent.ValueRW.Turn = 0f;
					intent.ValueRW.Thrust = 0f;
					intent.ValueRW.Fire = 0;
					continue;
				}

				Pose2D self = pose.ValueRO.Value;
				Pose2D tp = em.GetComponentData<WorldPose>(t).Value;

				float2 toT = tp.Position - self.Position;
				float distSq = math.lengthsq(toT);

				// Desired heading angle
				float desired = math.atan2(toT.y, toT.x);
				// Your forward vector uses theta -> (-sin, cos), which corresponds to "angle from +Y".
				// To keep consistent with your model:
				// forward = (-sin(theta), cos(theta))  ==> theta=0 points +Y.
				// So desired theta should be angle-from-+Y: atan2(-x, y)
				float desiredTheta = math.atan2(-toT.x, toT.y);

				float delta = NormalizeAngle(desiredTheta - self.Theta);

				// Turn sign towards target
				float turn = math.clamp(delta * 2.0f, -1f, 1f); // gain=2 for snappier steering
				float thrust = 1f;

				// Fire gating: within range and within cone
				float2 forward = new float2(-math.sin(self.Theta), math.cos(self.Theta));
				float2 dir = math.normalizesafe(toT);
				float facing = math.dot(forward, dir);
				byte fire = (byte)((distSq <= FireRange * FireRange && facing >= FireConeCos) ? 1 : 0);

				intent.ValueRW.Turn = turn;
				intent.ValueRW.Thrust = thrust;
				intent.ValueRW.Fire = fire;
			}
		}

		[BurstCompile]
		private static float NormalizeAngle(float a)
		{
			// map to [-pi, pi]
			a = math.fmod(a + math.PI, 2f * math.PI);
			if (a < 0f) a += 2f * math.PI;
			return a - math.PI;
		}
	}

}