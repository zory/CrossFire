using Core.Physics;
using CrossFire.Ships;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class TargetingSample : MonoBehaviour
	{
		void Start()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			int id = 0;

			//Ship 1
			{
				ShipType type = ShipType.Sample1;
				byte team = 0;
				float2 worldPose = new float2(0, 0);
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = math.PI * 0.5f
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = id++,
					Type = type,
					Team = team,
					Pose = pose
				};

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);
			}

			//Ship 2
			{
				ShipType type = ShipType.Sample2;
				byte team = 0;
				float2 worldPose = new float2(0, -2);
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = math.PI * 0.5f
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = id++,
					Type = type,
					Team = team,
					Pose = pose
				};

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);
			}

			//Ship 3
			{
				ShipType type = ShipType.Sample3;
				byte team = 1;
				float2 worldPose = new float2(-5, 2);
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = math.PI * 0.5f
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = id++,
					Type = type,
					Team = team,
					Pose = pose
				};

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);
			}

			//Ship 4
			{
				ShipType type = ShipType.Sample3;
				byte team = 1;
				float2 worldPose = new float2(5, 2);
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = math.PI * 0.5f
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = id++,
					Type = type,
					Team = team,
					Pose = pose
				};

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);
			}
		}
	}
}
