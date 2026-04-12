using CrossFire.Core;
using Core.Physics;
using Unity.Entities;

namespace CrossFire.App
{
	/// <summary>
	/// Presentation-layer pipeline that runs unconditionally every frame inside
	/// <see cref="PresentationSystemGroup"/>, independent of the simulation pause state.
	/// This ensures entity colours and debug overlays remain live while the gameplay
	/// simulation is frozen (e.g. during scene editing via <see cref="SimulationEditingTool"/>).
	/// </summary>
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial class AppPresentationPipeline : ComponentSystemGroup
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			EnableSystemSorting = false;

			World world = World;

			AddUnmanaged<ColorPresentationSystem>(world);
			AddUnmanaged<CollisionDebugSystem>(world);
		}

		private void AddUnmanaged<T>(World world) where T : unmanaged, ISystem
		{
			SystemHandle handle = world.GetOrCreateSystem<T>();
			AddSystemToUpdateList(handle);
		}
	}
}
