using Core.Physics;
using CrossFire.Ships;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class CollisionSample : MonoBehaviour
	{
		public int ShipCount = 20;
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			float2 originPoint = float2.zero;
			float distanceFromOrigin = 5f;
			for (int i = 0; i < ShipCount; i++)
			{
				ShipType type = ShipType.Sample1;
				byte team = 0;
				float spawningAngle = (i / (float)ShipCount) * (math.PI * 2f);
				float2 worldPose = originPoint + new float2(math.cos(spawningAngle), math.sin(spawningAngle)) * distanceFromOrigin;
				float2 dir = originPoint - worldPose;
				float angle = math.atan2(dir.y, dir.x) - math.PI * 0.5f;
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = angle
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = i,
					Type = type,
					Team = team,
					Pose = pose
				};

				EntityQuery query = entityManager.CreateEntityQuery(typeof(SpawnShipsCommandBufferTag));
				Entity entity = query.GetSingletonEntity();
				DynamicBuffer<SpawnShipsCommand> commandBuffer = entityManager.GetBuffer<SpawnShipsCommand>(entity);
				commandBuffer.Add(command);
			}

			{
				ShipType type = ShipType.Sample2;
				byte team = 1;
				float2 worldPose = originPoint;
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					ThetaRad = math.PI * 0.5f
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = ShipCount,
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