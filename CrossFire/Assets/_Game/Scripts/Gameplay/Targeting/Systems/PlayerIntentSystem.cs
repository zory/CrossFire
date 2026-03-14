using CrossFire.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace CrossFire.Targeting
{
	[DisableAutoCreation]
	//[BurstCompile]
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

		//[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _inputQuery.GetSingletonEntity();
			DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(commandEntity);

			if (commandBuffer.IsEmpty)
			{
				return;
			}

			ShipControlIntentCommand command = commandBuffer[commandBuffer.Length - 1];
			commandBuffer.Clear();

			float turn = math.clamp(command.Turn, -1f, 1f);
			float thrust = math.clamp(command.Thrust, -1f, 1f);
			byte fire = (byte)(command.Fire ? 1 : 0);

			foreach (RefRW<ControlIntent> controlIntent in
					 SystemAPI.Query<RefRW<ControlIntent>>().
						WithAll<ControlledTag>())
			{
				controlIntent.ValueRW.Turn = turn;
				controlIntent.ValueRW.Thrust = thrust;
				controlIntent.ValueRW.Fire = fire;
			} 
		}
	}
}
