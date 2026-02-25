using NUnit.Framework;
using Unity.Entities;

public class EcsEditModeSmokeTest
{
	[Test]
	public void CanCreateAndDisposeWorld_AndEntityManagerWorks()
	{
		using World world = new World("TestWorld");
		EntityManager entityManager = world.EntityManager;

		Entity entity = entityManager.CreateEntity();
		Assert.IsTrue(entityManager.Exists(entity));

		entityManager.DestroyEntity(entity);
		Assert.IsFalse(entityManager.Exists(entity));
	}
}