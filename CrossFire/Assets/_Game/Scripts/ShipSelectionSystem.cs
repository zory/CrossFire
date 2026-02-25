using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrossFire.Ships
{
	public struct SelectionRequestBufferTag : IComponentData { }

	public struct SelectionRequestCommand : IBufferElementData
	{
		public float2 WorldPosition;
		public float PickRadius;

		public override string ToString()
		{
			return
				string.Format(
					"SelectionRequestCommand. " +
					"WorldPosition:{0} " +
					"PickRadius:{1}",
					WorldPosition, PickRadius
				);
		}
	}

	public partial struct ClickPickRequestBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<SelectionRequestBufferTag>(entity);
			state.EntityManager.AddBuffer<SelectionRequestCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}

	public partial struct ShipSelectionSystem : ISystem
	{
		private EntityQuery _requestQuery;

		public void OnCreate(ref SystemState state)
		{
			_requestQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(SelectionRequestBufferTag),
					typeof(SelectionRequestCommand)
				}
			});

			state.RequireForUpdate(_requestQuery);
		}

		public void OnUpdate(ref SystemState state)
		{
			EntityManager entityManager = state.EntityManager;

			Entity commandEntity = _requestQuery.GetSingletonEntity();
			DynamicBuffer<SelectionRequestCommand> commandBuffer = entityManager.GetBuffer<SelectionRequestCommand>(commandEntity);
			if (commandBuffer.IsEmpty)
				return;

			NativeArray<SelectionRequestCommand> commands = commandBuffer.ToNativeArray(Allocator.Temp);
			commandBuffer.Clear();

			EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

			// Deselect everything
			NativeArray<Entity> currentlyControlled = SystemAPI.QueryBuilder().WithAll<ControlledTag>().Build().ToEntityArray(Allocator.Temp);
			for (int i = 0; i < currentlyControlled.Length; i++)
			{
				//This is debug colouring for selected units
				float4 nativeColor = entityManager.GetComponentData<NativeColor>(currentlyControlled[i]).Value;
				ShipsSpawnSystem.SetColor(entityManager, currentlyControlled[i], nativeColor);
				
				ecb.RemoveComponent<ControlledTag>(currentlyControlled[i]);
			}
			currentlyControlled.Dispose();

			for (int index = 0; index < commands.Length; index++)
			{
				SelectionRequestCommand command = commands[index];

				Entity bestEntity = GetClosestSelectableEntity(ref state, command.WorldPosition, command.PickRadius);

				//Select current selected
				if (bestEntity != Entity.Null)
				{
					//This is debug colouring for selected units
					ShipsSpawnSystem.SetColor(entityManager, bestEntity, new float4(0, 0, 0, 255));
					ecb.AddComponent<ControlledTag>(bestEntity);
				}
			}

			ecb.Playback(entityManager);
			ecb.Dispose();

			commands.Dispose();
		}

		private Entity GetClosestSelectableEntity(ref SystemState state, float2 worldPosition, float pickRadius)
		{
			Entity result = Entity.Null;

			float pickRadiusSq = pickRadius * pickRadius;
			float bestDistanceSq = pickRadiusSq;

			foreach ((RefRO<LocalTransform> localTransform, Entity entity) in
				SystemAPI.Query<RefRO<LocalTransform>>()
					.WithAll<SelectableTag>()
					.WithEntityAccess())
			{
				float2 shipPosition = new float2(localTransform.ValueRO.Position.x, localTransform.ValueRO.Position.y);
				float2 delta = shipPosition - worldPosition;
				float distanceSq = math.dot(delta, delta);

				if (distanceSq <= bestDistanceSq)
				{
					bestDistanceSq = distanceSq;
					result = entity;
				}
			}

			return result;
		}
	}
}
