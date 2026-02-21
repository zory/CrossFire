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

	[Header("Rendering")]
	public Material sharedMaterial;
	public float shipScale = 0.35f;
	public Color[] teamColors;

	// --- Simulation buffers (double-buffered) ---
	NativeArray<float2> posA, posB;
	NativeArray<float2> velA, velB;

	NativeArray<float2> posRead, posWrite;
	NativeArray<float2> velRead, velWrite;

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
		RenderTeams(posWrite, velWrite);

		var tmpP = posRead; posRead = posWrite; posWrite = tmpP;
		var tmpV = velRead; velRead = velWrite; velWrite = tmpV;
	}

	void OnDisable()
	{
		handle.Complete();

		if (posA.IsCreated) posA.Dispose();
		if (posB.IsCreated) posB.Dispose();
		if (velA.IsCreated) velA.Dispose();
		if (velB.IsCreated) velB.Dispose();

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
		// Collect areas per team (can have multiple areas per team)
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

		for (int t = 0; t < teamCount; t++)
		{
			int start = teamOffsets[t];
			int count = teamCounts[t];

			if (perTeam[t].Count == 0)
			{
				// Fallback: spawn in circle if no area assigned
				for (int k = 0; k < count; k++)
				{
					float2 p = rng.NextFloat2Direction() * rng.NextFloat(0f, fallbackSpawnRadius);
					posRead[start + k] = p;
					velRead[start + k] = rng.NextFloat2Direction() * 0.5f;
				}
				continue;
			}

			// Weighted pick by area size
			float totalW = 0f;
			float[] weights = new float[perTeam[t].Count];
			for (int a = 0; a < perTeam[t].Count; a++)
			{
				var b = perTeam[t][a].box.bounds;
				float w = Mathf.Max(0.0001f, b.size.x * b.size.y);
				weights[a] = w;
				totalW += w;
			}

			for (int k = 0; k < count; k++)
			{
				int chosen = 0;
				float r = rng.NextFloat(0f, totalW);
				for (int a = 0; a < weights.Length; a++)
				{
					r -= weights[a];
					if (r <= 0f) { chosen = a; break; }
				}

				Bounds bnd = perTeam[t][chosen].box.bounds;
				float x = rng.NextFloat(bnd.min.x, bnd.max.x);
				float y = rng.NextFloat(bnd.min.y, bnd.max.y);

				posRead[start + k] = new float2(x, y);
				velRead[start + k] = rng.NextFloat2Direction() * 0.5f;
			}
		}
	}

	void RenderTeams(NativeArray<float2> pArr, NativeArray<float2> vArr)
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

			RenderRange(pArr, vArr, start, count, sharedMaterial, mpb);
		}
	}

	void RenderRange(
		NativeArray<float2> pArr,
		NativeArray<float2> vArr,
		int start,
		int count,
		Material mat,
		MaterialPropertyBlock props)
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
				float2 v = vArr[idx];

				float angleDeg = math.degrees(math.atan2(v.y, v.x)) - 90f;
				Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);

				matrices[i] = Matrix4x4.TRS(
					new Vector3(p.x, p.y, 0f),
					rot,
					new Vector3(shipScale, shipScale, 1f));
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

		[ReadOnly] public NativeArray<float2> posRead;
		[ReadOnly] public NativeArray<float2> velRead;

		public NativeArray<float2> posWrite;
		public NativeArray<float2> velWrite;

		[ReadOnly] public NativeParallelMultiHashMap<int, int> grid;
		public NativeArray<byte> hitPlayer; // write only element i

		public void Execute(int i)
		{
			float2 p = posRead[i];
			float2 v = velRead[i];

			// Simple seek-player steering (placeholder)
			float2 toPlayer = playerPos - p;
			float distSq = math.lengthsq(toPlayer);
			float2 desiredDir = distSq > 1e-6f ? math.normalize(toPlayer) : new float2(0, 1);
			float2 desiredVel = desiredDir * speed;
			v = math.lerp(v, desiredVel, math.saturate(steerStrength * dt));

			// Collide with neighbors (separate self only)
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

							float2 d = p - posRead[j];
							float dsq = math.lengthsq(d);
							if (dsq < 1e-8f || dsq >= minDistSq) continue;

							float dist = math.sqrt(dsq);
							float overlap = (minDist - dist);
							float2 n = d / dist;

							push += n * overlap;

						} while (grid.TryGetNextValue(out j, ref it));
					}
				}

			if (!push.Equals(0f))
			{
				p += push * (collisionPush * dt);
				// mild velocity bias away from crowd
				float pushLenSq = math.lengthsq(push);
				if (pushLenSq > 1e-8f)
					v += (push / math.sqrt(pushLenSq)) * (0.5f * collisionPush * dt);
			}

			// Player overlap (flag + optional push-away)
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

			// Integrate
			p += v * dt;

			posWrite[i] = p;
			velWrite[i] = v;
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