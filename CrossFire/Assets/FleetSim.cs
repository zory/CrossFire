using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Burst-first fleet sim:
/// - Player is a normal GameObject (Transform reference)
/// - Ships are simulated in Burst using NativeArrays (no per-ship GameObjects)
/// - Ships are partitioned into contiguous team ranges [teamOffsets[t], teamOffsets[t]+teamCounts[t])
/// - Teams spawn inside TeamSpawnArea BoxCollider2D volumes placed in the scene
/// - Enemy-enemy collisions via grid broadphase (self-only separation; parallel safe)
/// - Enemy-player overlap flag set per ship (hitPlayer[i])
/// - Rendering via DrawMeshInstanced, per-team colors via MaterialPropertyBlock
/// </summary>
public class FleetSim : MonoBehaviour
{
	[Header("Refs")]
	public Transform player;

	[Header("Ship Size (world units)")]
	public Vector2 shipSize = new Vector2(0.35f, 0.8f);

	[Header("Turning")]
	public float turnSpeedDegPerSec = 180f; // like player


	[Header("Counts")]
	[Range(2, 8)] public int teamCount = 2;
	[Range(100, 50000)] public int shipCount = 5000;

	[Header("Spawn")]
	public int spawnSeed = 12345;
	public float fallbackSpawnRadius = 40f;
	public TeamSpawnArea[] spawnAreas; // if empty, will auto-FindObjectsOfType

	[Header("Steering (placeholder AI: seek player)")]
	public float shipSpeed = 5f;
	public float steerStrength = 6f;

	[Header("Collision")]
	public float shipRadius = 0.25f;
	public float playerRadius = 0.35f;
	public float collisionPush = 10f;

	[Header("Grid Broadphase")]
	public float cellSize = 0.6f; // >= 2*shipRadius recommended

	[Header("Spawn (no-overlap)")]
	public int maxSpawnAttemptsPerShip = 25;
	public float spawnSeparationMultiplier = 1.0f; // 1.0 => minDist = 2*shipRadius

	[Header("Rendering")]
	public Material sharedMaterial;
	public Color[] teamColors;

	// --- Simulation buffers (double-buffered) ---
	NativeArray<float2> posA, posB;
	NativeArray<float2> velA, velB;

	NativeArray<float2> posRead, posWrite;
	NativeArray<float2> velRead, velWrite;

	NativeArray<float> angA, angB;
	NativeArray<float> angRead, angWrite;

	// --- Team data ---
	NativeArray<byte> teamId;      // ship -> team
	NativeArray<int> teamOffsets;  // team -> start index
	NativeArray<int> teamCounts;   // team -> count

	// --- Broadphase ---
	NativeParallelMultiHashMap<int, int> grid; // cellHash -> ship index

	// --- Player contact flag (per ship) ---
	NativeArray<byte> hitPlayer;

	JobHandle handle;

	// --- Rendering ---
	Mesh quadMesh;
	Matrix4x4[] matrices;
	const int BatchSize = 1023;

	MaterialPropertyBlock mpb;
	static readonly int ColorId = Shader.PropertyToID("_Color");
	static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

	void OnEnable()
	{
		if (!player) player = GameObject.Find("Player")?.transform;

		// Auto-find spawn areas if not assigned
		if (spawnAreas == null || spawnAreas.Length == 0)
			spawnAreas = FindObjectsOfType<TeamSpawnArea>();

		// Colors default
		if (teamColors == null || teamColors.Length != teamCount)
		{
			teamColors = new Color[teamCount];
			for (int t = 0; t < teamCount; t++)
				teamColors[t] = Color.HSVToRGB((float)t / math.max(1, teamCount), 0.8f, 1f);
		}

		// Allocate buffers
		posA = new NativeArray<float2>(shipCount, Allocator.Persistent);
		posB = new NativeArray<float2>(shipCount, Allocator.Persistent);
		velA = new NativeArray<float2>(shipCount, Allocator.Persistent);
		velB = new NativeArray<float2>(shipCount, Allocator.Persistent);

		posRead = posA; posWrite = posB;
		velRead = velA; velWrite = velB;

		angA = new NativeArray<float>(shipCount, Allocator.Persistent);
		angB = new NativeArray<float>(shipCount, Allocator.Persistent);

		angRead = angA; angWrite = angB;

		teamId = new NativeArray<byte>(shipCount, Allocator.Persistent);
		teamOffsets = new NativeArray<int>(teamCount, Allocator.Persistent);
		teamCounts = new NativeArray<int>(teamCount, Allocator.Persistent);

		// Broadphase structures
		grid = new NativeParallelMultiHashMap<int, int>(shipCount * 2, Allocator.Persistent);
		hitPlayer = new NativeArray<byte>(shipCount, Allocator.Persistent);

		// Partition ships into contiguous team ranges
		int baseCount = shipCount / teamCount;
		int remainder = shipCount % teamCount;

		int idx = 0;
		for (int t = 0; t < teamCount; t++)
		{
			int c = baseCount + (t < remainder ? 1 : 0);
			teamOffsets[t] = idx;
			teamCounts[t] = c;
			for (int k = 0; k < c; k++)
				teamId[idx + k] = (byte)t;
			idx += c;
		}

		// Spawn ships into designer-defined areas
		SpawnTeams();

		// Sync write buffers initially
		posWrite.CopyFrom(posRead);
		velWrite.CopyFrom(velRead);
		angWrite.CopyFrom(angRead);

		// Rendering init
		quadMesh = BuildQuadMesh();
		matrices = new Matrix4x4[BatchSize];
		mpb = new MaterialPropertyBlock();
	}

	void Update()
	{
		if (!posRead.IsCreated) return;
		handle.Complete();

		// Clear broadphase & hit flags
		grid.Clear();
		for (int i = 0; i < shipCount; i++) hitPlayer[i] = 0;

		// Build grid from READ positions
		var buildGrid = new BuildGridJob
		{
			cellSize = cellSize,
			pos = posRead,
			gridWriter = grid.AsParallelWriter()
		}.ScheduleParallel(shipCount, 256, default);

		// Sim to WRITE buffers
		float2 playerPos = player ? new float2(player.position.x, player.position.y) : float2.zero;

		var sim = new SimJob
		{
			dt = Time.deltaTime,
			playerPos = playerPos,
			speed = shipSpeed,
			steerStrength = steerStrength,

			shipRadius = shipRadius,
			playerRadius = playerRadius,
			collisionPush = collisionPush,
			cellSize = cellSize,

			posRead = posRead,
			velRead = velRead,
			posWrite = posWrite,
			velWrite = velWrite,
			turnSpeedRad = math.radians(turnSpeedDegPerSec),
			angRead = angRead,
			angWrite = angWrite,

			grid = grid,
			hitPlayer = hitPlayer
		}.ScheduleParallel(shipCount, 256, buildGrid);

		handle = sim;
	}

	void LateUpdate()
	{
		if (!posRead.IsCreated) return;
		handle.Complete();

		// Render newest written buffers (posWrite/velWrite), then swap
		RenderTeams(posWrite, angWrite);

		var tmpP = posRead; posRead = posWrite; posWrite = tmpP;
		var tmpV = velRead; velRead = velWrite; velWrite = tmpV;
		var tmpA = angRead; angRead = angWrite; angWrite = tmpA;
	}

	void OnDisable()
	{
		handle.Complete();

		if (posA.IsCreated) posA.Dispose();
		if (posB.IsCreated) posB.Dispose();
		if (velA.IsCreated) velA.Dispose();
		if (velB.IsCreated) velB.Dispose();

		if (angA.IsCreated) angA.Dispose();
		if (angB.IsCreated) angB.Dispose();

		if (teamId.IsCreated) teamId.Dispose();
		if (teamOffsets.IsCreated) teamOffsets.Dispose();
		if (teamCounts.IsCreated) teamCounts.Dispose();

		if (grid.IsCreated) grid.Dispose();
		if (hitPlayer.IsCreated) hitPlayer.Dispose();
	}

	void OnDestroy()
	{
		OnDisable();
	}

	void SpawnTeams()
	{
		// Collect areas per team (can have multiple per team)
		var perTeam = new List<TeamSpawnArea>[teamCount];
		for (int t = 0; t < teamCount; t++) perTeam[t] = new List<TeamSpawnArea>();

		if (spawnAreas != null)
		{
			for (int i = 0; i < spawnAreas.Length; i++)
			{
				var a = spawnAreas[i];
				if (!a || !a.box) continue;
				if (a.teamId >= 0 && a.teamId < teamCount)
					perTeam[a.teamId].Add(a);
			}
		}

		var rng = new Unity.Mathematics.Random((uint)math.max(1, spawnSeed));

		float minDist = (shipRadius * 2f) * math.max(0.1f, spawnSeparationMultiplier);
		float minDistSq = minDist * minDist;

		for (int t = 0; t < teamCount; t++)
		{
			int start = teamOffsets[t];
			int count = teamCounts[t];

			// Determine spawn source (areas or fallback circle)
			bool hasAreas = perTeam[t].Count > 0;

			// Weighted pick by area size
			float totalW = 0f;
			float[] weights = null;
			if (hasAreas)
			{
				weights = new float[perTeam[t].Count];
				for (int a = 0; a < perTeam[t].Count; a++)
				{
					var b = perTeam[t][a].box.bounds;
					float w = Mathf.Max(0.0001f, b.size.x * b.size.y);
					weights[a] = w;
					totalW += w;
				}
			}

			// Occupancy grid for THIS TEAM spawn (team-local, avoids team self-overlap)
			// cell size = minDist => only need to check neighboring cells
			float cellSize = minDist;
			var grid = new Dictionary<long, List<int>>(count * 2);

			int placed = 0;

			for (int k = 0; k < count; k++)
			{
				bool ok = false;
				float2 candidate = 0f;

				for (int attempt = 0; attempt < maxSpawnAttemptsPerShip; attempt++)
				{
					candidate = hasAreas
						? SamplePointInTeamAreas(perTeam[t], weights, totalW, ref rng)
						: (rng.NextFloat2Direction() * rng.NextFloat(0f, fallbackSpawnRadius));

					if (IsFree(candidate, start, placed, cellSize, minDistSq, posRead, grid))
					{
						ok = true;
						break;
					}
				}

				if (!ok)
				{
					// Fallback: accept overlap (rare) OR relax minDist if you prefer.
					// Here: accept last candidate to guarantee progress.
					// You can also log once for debugging.
					// Debug.LogWarning($"Team {t}: spawn packing too dense; accepting overlap for ship {k}.");
				}

				int idx = start + k;
				posRead[idx] = candidate;
				velRead[idx] = rng.NextFloat2Direction() * 0.5f;
				float angleRad = rng.NextFloat(0f, math.PI * 2f);
				angRead[idx] = angleRad;

				AddToGrid(candidate, idx, cellSize, grid);

				placed++;
			}
		}
	}

	void RenderTeams(NativeArray<float2> pArr, NativeArray<float> aArr)
	{
		if (!sharedMaterial || !quadMesh) return;

		for (int t = 0; t < teamCount; t++)
		{
			// Set both common color property names (Built-in + URP)
			mpb.Clear();
			mpb.SetColor(ColorId, teamColors[t]);
			mpb.SetColor(BaseColorId, teamColors[t]);

			int start = teamOffsets[t];
			int count = teamCounts[t];

			RenderRange(pArr, aArr, start, count, sharedMaterial, mpb);
		}
	}

	void RenderRange(NativeArray<float2> pArr, NativeArray<float> aArr, int start, int count, Material mat, MaterialPropertyBlock props)
	{
		int remaining = count;
		int offset = start;

		while (remaining > 0)
		{
			int drawCount = Mathf.Min(remaining, BatchSize);

			for (int i = 0; i < drawCount; i++)
			{
				int idx = offset + i;
				float2 p = pArr[idx];
				float2 v = aArr[idx];

				float angleDeg = math.degrees(aArr[idx]);   // NOT from velocity
				Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);

				matrices[i] = Matrix4x4.TRS(
					new Vector3(p.x, p.y, 0f),
					rot,
					new Vector3(shipSize.x, shipSize.y, 1f));
			}

			Graphics.DrawMeshInstanced(quadMesh, 0, mat, matrices, drawCount, props);

			offset += drawCount;
			remaining -= drawCount;
		}
	}

	static Mesh BuildQuadMesh()
	{
		var mesh = new Mesh();
		mesh.vertices = new[]
		{
			new Vector3(-0.5f, -0.5f, 0),
			new Vector3(-0.5f,  0.5f, 0),
			new Vector3( 0.5f,  0.5f, 0),
			new Vector3( 0.5f, -0.5f, 0),
		};
		mesh.uv = new[]
		{
			new Vector2(0,0),
			new Vector2(0,1),
			new Vector2(1,1),
			new Vector2(1,0),
		};
		mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
		mesh.RecalculateNormals();
		return mesh;
	}

	[BurstCompile]
	struct BuildGridJob : IJobFor
	{
		public float cellSize;
		[ReadOnly] public NativeArray<float2> pos;
		public NativeParallelMultiHashMap<int, int>.ParallelWriter gridWriter;

		public void Execute(int i)
		{
			int2 cell = WorldToCell(pos[i], cellSize);
			gridWriter.Add(HashCell(cell), i);
		}
	}

	[BurstCompile]
	struct SimJob : IJobFor
	{
		public float dt;
		public float2 playerPos;
		public float speed;
		public float steerStrength;

		public float shipRadius;
		public float playerRadius;
		public float collisionPush;
		public float cellSize;
		public float turnSpeedRad; // set from turnSpeedDegPerSec

		[ReadOnly] public NativeArray<float2> posRead;
		[ReadOnly] public NativeArray<float2> velRead;
		[ReadOnly] public NativeArray<float> angRead;

		public NativeArray<float2> posWrite;
		public NativeArray<float2> velWrite;
		public NativeArray<float> angWrite;

		[ReadOnly] public NativeParallelMultiHashMap<int, int> grid;
		public NativeArray<byte> hitPlayer; // write only element i

		public void Execute(int i)
		{
			// Read state
			float2 p = posRead[i];
			float theta = angRead[i]; // radians, 0 faces +Y, +theta is CCW

			// --- Turn toward player (forward+turn model) ---
			float2 toPlayer = playerPos - p;

			// Forward for our angle convention (theta=0 => (0,1), theta=+90deg => (-1,0))
			float2 forward = new float2(-math.sin(theta), math.cos(theta));

			float distSq = math.lengthsq(toPlayer);
			float2 desiredDir = distSq > 1e-6f ? math.normalize(toPlayer) : forward;

			// Signed angle from forward -> desired (CCW positive)
			float cross = forward.x * desiredDir.y - forward.y * desiredDir.x; // cross2D(forward, desired)
			float dot = math.clamp(math.dot(forward, desiredDir), -1f, 1f);
			float angleError = math.atan2(cross, dot); // radians

			// Apply limited turn rate
			float maxTurn = turnSpeedRad * dt;
			theta += math.clamp(angleError, -maxTurn, maxTurn);

			// Recompute forward after turning
			forward = new float2(-math.sin(theta), math.cos(theta));

			// Constant forward speed
			float2 v = forward * speed;

			// Integrate
			p += v * dt;

			// --- Collision: ship-ship (separate self only) ---
			int2 myCell = WorldToCell(p, cellSize);
			float minDist = shipRadius * 2f;
			float minDistSq = minDist * minDist;

			float2 push = 0f;

			for (int oy = -1; oy <= 1; oy++)
				for (int ox = -1; ox <= 1; ox++)
				{
					int2 c = myCell + new int2(ox, oy);
					int hash = HashCell(c);

					NativeParallelMultiHashMapIterator<int> it;
					int j;
					if (grid.TryGetFirstValue(hash, out j, out it))
					{
						do
						{
							if (j == i) continue;

							float2 pj = posRead[j];
							float2 d = p - pj;
							float dsq = math.lengthsq(d);

							if (dsq < 1e-8f || dsq >= minDistSq) continue;

							float dist = math.sqrt(dsq);
							float overlap = minDist - dist;
							float2 n = d / dist;

							push += n * overlap;

						} while (grid.TryGetNextValue(out j, ref it));
					}
				}

			if (!push.Equals(0f))
			{
				p += push * (collisionPush * dt);

				float pushLenSq = math.lengthsq(push);
				if (pushLenSq > 1e-8f)
				{
					float2 pn = push / math.sqrt(pushLenSq);
					v += pn * (0.5f * collisionPush * dt);
				}
			}

			// --- Collision: ship-player (flag + optional push away) ---
			float minPlayerDist = shipRadius + playerRadius;
			float2 dp = p - playerPos;
			float dpsq = math.lengthsq(dp);

			if (dpsq < minPlayerDist * minPlayerDist)
			{
				hitPlayer[i] = 1;

				if (dpsq > 1e-6f)
				{
					float dist = math.sqrt(dpsq);
					float2 n = dp / dist;

					p += n * (minPlayerDist - dist) * 0.5f;
					v += n * (collisionPush * dt);
				}
			}

			// Write
			posWrite[i] = p;
			velWrite[i] = v;
			angWrite[i] = theta;
		}
	}

	float2 SamplePointInTeamAreas(List<TeamSpawnArea> areas, float[] weights, float totalW, ref Unity.Mathematics.Random rng)
	{
		int chosen = 0;
		float r = rng.NextFloat(0f, totalW);
		for (int a = 0; a < weights.Length; a++)
		{
			r -= weights[a];
			if (r <= 0f) { chosen = a; break; }
		}

		Bounds b = areas[chosen].box.bounds;
		float x = rng.NextFloat(b.min.x, b.max.x);
		float y = rng.NextFloat(b.min.y, b.max.y);
		return new float2(x, y);
	}

	bool IsFree(
		float2 p,
		int teamStart,
		int placedSoFar,
		float cellSize,
		float minDistSq,
		NativeArray<float2> positions,
		Dictionary<long, List<int>> grid)
	{
		int2 c = WorldToCell(p, cellSize);

		// check 3x3 neighbor cells
		for (int oy = -1; oy <= 1; oy++)
			for (int ox = -1; ox <= 1; ox++)
			{
				int2 nc = c + new int2(ox, oy);
				long key = HashCell64(nc);

				if (!grid.TryGetValue(key, out var list))
					continue;

				for (int i = 0; i < list.Count; i++)
				{
					int idx = list[i];
					float2 d = p - positions[idx];
					if (math.lengthsq(d) < minDistSq)
						return false;
				}
			}

		return true;
	}

	void AddToGrid(float2 p, int idx, float cellSize, Dictionary<long, List<int>> grid)
	{
		int2 c = WorldToCell(p, cellSize);
		long key = HashCell64(c);

		if (!grid.TryGetValue(key, out var list))
		{
			list = new List<int>(4);
			grid.Add(key, list);
		}
		list.Add(idx);
	}

	static long HashCell64(int2 c)
	{
		// Pack two signed 32-bit ints into one 64-bit key
		unchecked
		{
			return ((long)c.x << 32) ^ (uint)c.y;
		}
	}

	static int2 WorldToCell(float2 p, float cellSize)
	{
		return (int2)math.floor(p / cellSize);
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