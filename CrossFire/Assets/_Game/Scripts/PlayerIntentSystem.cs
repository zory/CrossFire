using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossFire.Ships
{
	public struct ShipControlIntentCommandBufferTag : IComponentData
	{
	}

	public struct ShipControlIntentCommand : IBufferElementData
	{
		public float Turn;   // -1..+1
		public float Thrust; // -1..+1
		public bool Fire;

		public override string ToString()
		{
			return
				string.Format(
					"ShipMoveCommand. " +
					"Turn:{0} " +
					"Thrust:{1} " +
					"Fire:{2} ",
					Turn, Thrust, Fire
				);
		}
	}

	public partial struct ShipControlIntentCommandBufferSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<ShipControlIntentCommandBufferTag>(entity);
			state.EntityManager.AddBuffer<ShipControlIntentCommand>(entity);
		}

		public void OnUpdate(ref SystemState state) { }
	}


	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(SnapshotSystem))]
	[BurstCompile]
	public partial struct PlayerIntentSystem : ISystem
	{
		private EntityQuery _inputQuery;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_inputQuery = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(ShipControlIntentCommandBufferTag),
					typeof(ShipControlIntentCommand)
				}
			});

			state.RequireForUpdate(_inputQuery);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var em = state.EntityManager;

			Entity cmdEntity = _inputQuery.GetSingletonEntity();
			DynamicBuffer<ShipControlIntentCommand> buf = em.GetBuffer<ShipControlIntentCommand>(cmdEntity);

			if (buf.IsEmpty)
			{
				// No new input this frame: do nothing.
				// Controlled ships keep their last intent unless you explicitly zero them elsewhere.
				return;
			}

			// Last command wins
			var cmd = buf[buf.Length - 1];
			buf.Clear();

			float turn = math.clamp(cmd.Turn, -1f, 1f);
			float thrust = math.clamp(cmd.Thrust, -1f, 1f);
			byte fire = (byte)(cmd.Fire ? 1 : 0);

			foreach (RefRW<ShipIntent> intent in
					 SystemAPI.Query<RefRW<ShipIntent>>().WithAll<ControlledTag>())
			{
				intent.ValueRW.Turn = turn;
				intent.ValueRW.Thrust = thrust;
				intent.ValueRW.Fire = fire;
			}
		}
	}
}
