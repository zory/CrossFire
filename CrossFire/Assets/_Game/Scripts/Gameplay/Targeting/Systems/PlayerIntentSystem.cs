using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	[BurstCompile]
	public partial struct PlayerIntentSystem : ISystem
	{
		private EntityQuery _inputQuery;

		public void OnCreate(ref SystemState state)
		{
			_inputQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(ShipControlIntentCommandBufferTag),
					typeof(ShipControlIntentCommand)
				}
			});

			state.RequireForUpdate(_inputQuery);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var entityManager = state.EntityManager;

			Entity commandEntity = _inputQuery.GetSingletonEntity();
			DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(commandEntity);

			if (commandBuffer.IsEmpty)
			{
				// No new input this frame: do nothing.
				// Controlled ships keep their last intent unless you explicitly zero them elsewhere.
				return;
			}

			// Last command wins
			var cmd = commandBuffer[commandBuffer.Length - 1];
			commandBuffer.Clear();

			float turn = math.clamp(cmd.Turn, -1f, 1f);
			float thrust = math.clamp(cmd.Thrust, -1f, 1f);
			byte fire = (byte)(cmd.Fire ? 1 : 0);

			foreach (RefRW<ControlIntent> intent in SystemAPI.Query<RefRW<ControlIntent>>().WithAll<ControlledTag>())
			{
				intent.ValueRW.Turn = turn;
				intent.ValueRW.Thrust = thrust;
				intent.ValueRW.Fire = fire;
			}
		}
	}
}
