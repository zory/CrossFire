using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.TestTools;

public class EcsPlayModeSmokeTest
{
	[UnityTest]
	public IEnumerator CanCreateAndDisposeWorld_InPlayMode()
	{
		using World world = new World("PlayModeTestWorld");
		EntityManager entityManager = world.EntityManager;

		Entity entity = entityManager.CreateEntity();
		Assert.IsTrue(entityManager.Exists(entity));

		// Let one frame pass (proves it runs in PlayMode)
		yield return null;

		entityManager.DestroyEntity(entity);
		Assert.IsFalse(entityManager.Exists(entity));
	}
}