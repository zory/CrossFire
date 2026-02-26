using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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