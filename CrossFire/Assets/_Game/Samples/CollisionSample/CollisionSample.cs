using CrossFire.Ships;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using CrossFire.Physics;

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
				Color color = Color.white;
				float4 colorRGBA = new float4(color.r, color.g, color.b, color.a);
				float spawningAngle = (i / (float)ShipCount) * (math.PI * 2f);
				float2 worldPose = originPoint + new float2(math.cos(spawningAngle), math.sin(spawningAngle)) * distanceFromOrigin;
				float2 dir = originPoint - worldPose;
				float angle = math.atan2(dir.y, dir.x) - math.PI * 0.5f;
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					Theta = angle
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = i,
					Type = type,
					Team = team,
					ColorRGBA = colorRGBA,
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
				Color color = Color.green;
				float4 colorRGBA = new float4(color.r, color.g, color.b, color.a);
				float2 worldPose = originPoint;
				Pose2D pose = new Pose2D
				{
					Position = worldPose,
					Theta = 0
				};

				SpawnShipsCommand command = new SpawnShipsCommand()
				{
					Id = ShipCount,
					Type = type,
					Team = team,
					ColorRGBA = colorRGBA,
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