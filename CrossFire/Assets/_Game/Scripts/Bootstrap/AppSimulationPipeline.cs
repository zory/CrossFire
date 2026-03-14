using CrossFire.Combat;
using CrossFire.Core;
using CrossFire.Physics;
using CrossFire.Ships;
using CrossFire.Targeting;
using Unity.Entities;

namespace CrossFire.App
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial class AppSimulationPipeline : ComponentSystemGroup
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			World world = World;

			AddUnmanaged<CollisionEventBufferBootstrapSystem>(world); //InitializationSystemGroup?
			Add(world.GetOrCreateSystemManaged<ShipsSpawnCommandBufferSystem>());   //InitializationSystemGroup?  
			AddUnmanaged<ShipControlIntentCommandBufferSystem>(world);  //InitializationSystemGroup?

			// Before frame
			AddUnmanaged<SnapshotSystem>(world);

			// Spawn / frame start
			AddUnmanaged<ShipsSpawnSystem>(world);

			// Intent / decision
			AddUnmanaged<ClickPickRequestBufferSystem>(world);
			AddUnmanaged<ShipSelectionSystem>(world);
			AddUnmanaged<PlayerIntentSystem>(world);

			AddUnmanaged<TargetValidationSystem>(world);
			AddUnmanaged<ManualTargetApplySystem>(world);
			AddUnmanaged<TargetAcquireSystem>(world);
			AddUnmanaged<TargetRetargetTimerSystem>(world);
			AddUnmanaged<AIIntentSystem>(world);

			// Movement
			AddUnmanaged<ShipMovementSystem>(world);
			AddUnmanaged<WeaponCooldownSystem>(world);
			AddUnmanaged<WeaponFireSystem>(world);
			AddUnmanaged<LinearDampingSystem>(world);
			AddUnmanaged<AngularIntegrationSystem>(world);
			AddUnmanaged<PositionIntegrationSystem>(world);
			AddUnmanaged<MaxVelocityClampSystem>(world);

			// Physics
			AddUnmanaged<CollisionDetectionSystem>(world);

			// Combat / collision reactions
			AddUnmanaged<BulletUpdateSystem>(world);
			AddUnmanaged<BulletDamageOnCollisionSystem>(world);
			AddUnmanaged<BulletDestroyOnCollisionSystem>(world);
			AddUnmanaged<DeathSystem>(world);
			AddUnmanaged<PostPhysicsSystem>(world);   //before TransformSystemGroup?

			 // Cleanup / presentation
			AddUnmanaged<CollisionEventCleanupSystem>(world);

			AddUnmanaged<ColorPresentationSystem>(world); //PresentationSystemGroup

			SortSystems();
		}

		private void AddUnmanaged<T>(World world) where T : unmanaged, ISystem
		{
			var handle = world.GetOrCreateSystem<T>();
			AddSystemToUpdateList(handle);
		}
		private void Add(ComponentSystemBase system)
		{
			AddSystemToUpdateList(system);
		}
	}
}