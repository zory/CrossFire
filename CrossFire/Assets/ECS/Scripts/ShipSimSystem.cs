using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShipSnapshotSystem))]
[UpdateBefore(typeof(ShipTransformSyncSystem))]
public partial struct ShipSimSystem : ISystem
{
	NativeParallelMultiHashMap<int, Entity> grid;
	int gridCapacity;

	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<BattleConfig>();
		state.RequireForUpdate<PlayerInput>();
		state.RequireForUpdate<ControlledShip>();
		state.RequireForUpdate<ShipTag>();

		grid = default;
		gridCapacity = 0;
	}

	public void OnDestroy(ref SystemState state)
	{
		if (grid.IsCreated) grid.Dispose();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var cfg = SystemAPI.GetSingleton<BattleConfig>();
		var input = SystemAPI.GetSingleton<PlayerInput>();
		var controlled = SystemAPI.GetSingleton<ControlledShip>().Value;

		float dt = SystemAPI.Time.DeltaTime;

		// controlled ship target position (from snapshot)
		float2 targetPos = 0f;
		var prevLookupRO = SystemAPI.GetComponentLookup<ShipPrevPos>(true);
		if (controlled != Entity.Null && state.EntityManager.Exists(controlled) && prevLookupRO.HasComponent(controlled))
			targetPos = prevLookupRO[controlled].Value;

		// Ensure grid capacity
		int shipCount = SystemAPI.QueryBuilder().WithAll<ShipTag>().Build().CalculateEntityCount();
		int needed = math.max(16, shipCount * 2);

		if (!grid.IsCreated || needed > gridCapacity)
		{
			if (grid.IsCreated) grid.Dispose();
			grid = new NativeParallelMultiHashMap<int, Entity>(needed, Allocator.Persistent);
			gridCapacity = needed;
		}
		else
		{
			grid.Clear();
		}

		// 1) Build grid from ShipPrevPos
		var buildJob = new BuildGridJob
		{
			CellSize = cfg.CellSize,
			Writer = grid.AsParallelWriter()
		};
		var buildHandle = buildJob.ScheduleParallel(state.Dependency);

		// 2) Sim job
		var prevLookup = SystemAPI.GetComponentLookup<ShipPrevPos>(true);

		var simJob = new SimJob
		{
			Dt = dt,
			CellSize = cfg.CellSize,

			Controlled = controlled,
			Input = input,
			TargetPos = targetPos,

			PrevPosLookup = prevLookup,
			Grid = grid
		};

		state.Dependency = simJob.ScheduleParallel(buildHandle);
	}

	[BurstCompile]
	partial struct BuildGridJob : IJobEntity
	{
		public float CellSize;
		public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Writer;

		void Execute(Entity e, in ShipPrevPos prevPos, in ShipTag tag)
		{
			int2 cell = (int2)math.floor(prevPos.Value / CellSize);
			Writer.Add(HashCell(cell), e);
		}
	}

	[BurstCompile]
	partial struct SimJob : IJobEntity
	{
		public float Dt;
		public float CellSize;

		public Entity Controlled;
		public PlayerInput Input;  // copied into job
		public float2 TargetPos;   // controlled ship snapshot

		[ReadOnly] public ComponentLookup<ShipPrevPos> PrevPosLookup;
		[ReadOnly] public NativeParallelMultiHashMap<int, Entity> Grid;

		void Execute(
			Entity e,
			ref ShipPos pos,
			ref ShipAngle ang,
			in ShipSpeed speed,
			in ShipTurnSpeed turn,
			in ShipRadius radius,
			in ShipTag tag)
		{
			float2 p = pos.Value;
			float theta = ang.Value; // radians, 0 faces +Y, +CCW

			// Forward vector: theta=0 => +Y, +theta => CCW
			float2 forward = new float2(-math.sin(theta), math.cos(theta));

			// Controlled ship uses player input; others seek TargetPos
			if (e == Controlled)
			{
				// Turn and thrust (forward only)
				theta += (Input.Turn * turn.Value * Dt);

				forward = new float2(-math.sin(theta), math.cos(theta));
				float thrust = math.clamp(Input.Thrust, -1f, 1f);
				float2 v = forward * (speed.Value * thrust);
				p += v * Dt;
			}
			else
			{
				float2 toTarget = TargetPos - p;
				float distSq = math.lengthsq(toTarget);
				float2 desired = distSq > 1e-6f ? math.normalize(toTarget) : forward;

				float cross = forward.x * desired.y - forward.y * desired.x;
				float dot = math.clamp(math.dot(forward, desired), -1f, 1f);
				float angleError = math.atan2(cross, dot);

				float maxTurn = turn.Value * Dt;
				theta += math.clamp(angleError, -maxTurn, maxTurn);

				forward = new float2(-math.sin(theta), math.cos(theta));
				float2 v = forward * speed.Value;
				p += v * Dt;
			}

			// Separation collisions (read neighbors from ShipPrevPos snapshot)
			float minDist = radius.Value * 2f;
			float minDistSq = minDist * minDist;
			float2 push = 0f;

			int2 myCell = (int2)math.floor(p / CellSize);

			for (int oy = -1; oy <= 1; oy++)
				for (int ox = -1; ox <= 1; ox++)
				{
					int2 c = myCell + new int2(ox, oy);
					int hash = HashCell(c);

					NativeParallelMultiHashMapIterator<int> it;
					Entity other;
					if (Grid.TryGetFirstValue(hash, out other, out it))
					{
						do
						{
							if (other == e) continue;
							if (!PrevPosLookup.HasComponent(other)) continue;

							float2 op = PrevPosLookup[other].Value;
							float2 d = p - op;
							float dsq = math.lengthsq(d);

							if (dsq < 1e-8f || dsq >= minDistSq) continue;

							float dist = math.sqrt(dsq);
							float overlap = minDist - dist;
							float2 n = d / dist;
							push += n * overlap;

						} while (Grid.TryGetNextValue(out other, ref it));
					}
				}

			if (!push.Equals(0f))
			{
				float collisionPush = 10f;
				p += push * (collisionPush * Dt);
			}

			pos.Value = p;
			ang.Value = theta;
		}
	}

	static int HashCell(int2 c)
	{
		unchecked
		{
			int h = 17;
			h = h * 31 + c.x;
			h = h * 31 + c.y;
			return h;
		}
	}
}