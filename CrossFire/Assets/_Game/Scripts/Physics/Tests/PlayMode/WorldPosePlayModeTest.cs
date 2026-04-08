using Core.Physics;
using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Core.Physics.Tests.PlayMode
{
	public class WorldPosePlayModeTest
	{
		[UnityTest]
		public IEnumerator WorldPose_AddedToEntity_CanBeRetrievedAfterFrame()
		{
			using World world = new World("PhysicsPlayModeTest");
			EntityManager entityManager = world.EntityManager;

			Entity entity = entityManager.CreateEntity();
			WorldPose expectedPose = new WorldPose
			{
				Value = new Pose2D
				{
					Position = new float2(3f, 5f),
					ThetaRad = 1.2f
				}
			};
			entityManager.AddComponentData(entity, expectedPose);

			// Let one frame pass to prove the component survives a tick
			yield return null;

			WorldPose retrievedPose = entityManager.GetComponentData<WorldPose>(entity);
			Assert.AreEqual(expectedPose.Value.Position, retrievedPose.Value.Position);
			Assert.AreEqual(expectedPose.Value.ThetaRad, retrievedPose.Value.ThetaRad);
		}
	}
}
