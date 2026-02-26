using CrossFire;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

//Order:
//ShipSelectionSystem (after snapshots?)

//ShipsSpawnSystem
//SnapshotSystem
//ShipControlSystem
//CollisionSystem
//PostPhysicsSystem


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

	[Test]
	public void CanInstantiatePrefabAndCopyComponents_EditMode()
	{
		using World world = new World("EditModePrefabTest");
		EntityManager entityManager = world.EntityManager;

		// Create prefab entity and add some components expected to be copied
		Entity prefab = entityManager.CreateEntity();
		entityManager.AddComponentData(prefab, new StableId { Value = 1 });
		entityManager.AddComponentData(prefab, new TeamId { Value = 1 });
		entityManager.AddComponentData(prefab, new NativeColor { Value = new float4(1, 1, 1, 1) });

		// Instantiate
		Entity inst = entityManager.Instantiate(prefab);
		Assert.IsTrue(entityManager.Exists(inst));

		// Ensure components were copied
		Assert.IsTrue(entityManager.HasComponent<StableId>(inst));
		Assert.IsTrue(entityManager.HasComponent<TeamId>(inst));
		Assert.IsTrue(entityManager.HasComponent<NativeColor>(inst));

		Assert.AreEqual(1, entityManager.GetComponentData<StableId>(inst).Value);
		Assert.AreEqual(1, entityManager.GetComponentData<TeamId>(inst).Value);
		Assert.AreEqual(new float4(1, 1, 1, 1), entityManager.GetComponentData<NativeColor>(inst).Value);
	}
}