using Core.Physics;
using CrossFire.Ships;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class BulletCollisionSample : MonoBehaviour
	{
		public int ShipCount = 20;

		void Start()
		{
			float2 originPoint = float2.zero;
			float distanceFromOrigin = 5f;

			for (int i = 0; i < ShipCount; i++)
			{
				float spawningAngle = (i / (float)ShipCount) * (math.PI * 2f);
				float2 position = originPoint + new float2(math.cos(spawningAngle), math.sin(spawningAngle)) * distanceFromOrigin;
				float2 dir = originPoint - position;
				float angle = math.atan2(dir.y, dir.x) - math.PI * 0.5f;

				ShipSpawner.Spawn(ShipType.Sample1, team: 0, new Pose2D { Position = position, ThetaRad = angle });
			}

			ShipSpawner.Spawn(ShipType.Sample2, team: 1, new Pose2D { Position = originPoint, ThetaRad = math.PI * 0.5f });
		}
	}
}
