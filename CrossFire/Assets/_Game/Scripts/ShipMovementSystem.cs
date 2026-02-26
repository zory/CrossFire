using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrossFire.Ships
{
	public struct ShipControlIntentCommandBufferTag : IComponentData
	{
	}

	public struct ShipControlIntentCommand : IBufferElementData
	{
		public float Turn;   // -1..+1
		public float Thrust; // -1..+1
		public bool Fire;

		public override string ToString()
		{
			return
				string.Format(
					"ShipMoveCommand. " +
					"Turn:{0} " +
					"Thrust:{1} " +
					"Fire:{2} ",
					Turn, Thrust, Fire
				);
		}
	}

	public partial struct ShipControlIntentCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<ShipControlIntentCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<ShipControlIntentCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(SnapshotSystem))]
	[BurstCompile]
	public partial struct ShipControlSystem : ISystem
	{
		private EntityQuery _requestQuery;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(ShipControlIntentCommandBufferTag),
					typeof(ShipControlIntentCommand)
				}
			});

			state.RequireForUpdate(_requestQuery);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<ShipControlIntentCommand> commandBuffer = entityManager.GetBuffer<ShipControlIntentCommand>(commandEntity);
			if (commandBuffer.IsEmpty)
				return;

			// Take last command only (simplest behaviour)
			ShipControlIntentCommand command = commandBuffer[commandBuffer.Length - 1];
			commandBuffer.Clear();

			float deltaTime = SystemAPI.Time.DeltaTime;

			float turnSpeed = 3f;
			float thrustSpeed = 5f;

			foreach (RefRW<WorldPose> transform in
					 SystemAPI.Query<RefRW<WorldPose>>()
							  .WithAll<ControlledTag>())
			{
				float theta = transform.ValueRW.Value.Theta;
				float rotationDelta = command.Turn * turnSpeed * deltaTime;
				theta += rotationDelta;
				transform.ValueRW.Value.Theta = theta;

				float2 forward = new float2(-math.sin(theta), math.cos(theta));
				transform.ValueRW.Value.Position += forward * command.Thrust * thrustSpeed * deltaTime;
			}
		}
	}
}