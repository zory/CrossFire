using Core.Physics;
using CrossFire.Ships;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Samples
{
	public class TargetingSample : MonoBehaviour
	{
		void Start()
		{
			ShipSpawner.Spawn(ShipType.Sample1, team: 0, new Pose2D { Position = new float2(0f, 0f),  ThetaRad = math.PI * 0.5f });
			ShipSpawner.Spawn(ShipType.Sample2, team: 0, new Pose2D { Position = new float2(0f, -2f), ThetaRad = math.PI * 0.5f });
			ShipSpawner.Spawn(ShipType.Sample3, team: 1, new Pose2D { Position = new float2(-5f, 2f), ThetaRad = math.PI * 0.5f });
			ShipSpawner.Spawn(ShipType.Sample3, team: 1, new Pose2D { Position = new float2(5f, 2f),  ThetaRad = math.PI * 0.5f });
		}
	}
}
