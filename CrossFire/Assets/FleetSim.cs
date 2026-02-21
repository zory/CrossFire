using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FleetSim : MonoBehaviour
{
	[Header("Refs")]
	public Transform player;

	[Header("Enemy Count")]
	[Range(100, 50000)]
	public int enemyCount = 5000;

	[Header("Spawn")]
	public float spawnRadius = 40f;

	[Header("Enemy Movement")]
	public float enemySpeed = 5f;
	public float steerStrength = 6f;

	[Header("Collision")]
	public float enemyRadius = 0.25f;
	public float playerRadius = 0.35f;
	public float collisionPush = 10f; // how hard ships separate when overlapping

	[Header("Grid (Broadphase)")]
	public float cellSize = 0.6f; // should be >= 2*enemyRadius (start around 2.4*radius)

	[Header("Rendering")]
	public Material enemyMaterial;
	public float enemyScale = 0.35f;

	NativeArray<float2> posA, posB;
	NativeArray<float2> velA, velB;

	NativeArray<float2> posRead, posWrite;
	NativeArray<float2> velRead, velWrite;

	NativeParallelMultiHashMap<int, int> grid; // cellHash -> ship index
	NativeArray<byte> hitPlayer;               // 1 if enemy i overlaps player this frame

	JobHandle handle;

	Mesh quadMesh;
	Matrix4x4[] matrices;
	const int BatchSize = 1023;

	void OnDestroy()
	{
		OnDisable(); // ensures cleanup in editor domain reload / scene changes
	}

	void OnEnable()
	{
		if (!player) player = GameObject.Find("Player")?.transform;

		posA = new NativeArray<float2>(enemyCount, Allocator.Persistent);
		posB = new NativeArray<float2>(enemyCount, Allocator.Persistent);
		velA = new NativeArray<float2>(enemyCount, Allocator.Persistent);
		velB = new NativeArray<float2>(enemyCount, Allocator.Persistent);

		posRead = posA; posWrite = posB;
		velRead = velA; velWrite = velB;

		// grid capacity: at least enemyCount, more if you’ll insert multiple times (we insert once per ship)
		grid = new NativeParallelMultiHashMap<int, int>(enemyCount * 2, Allocator.Persistent);
		hitPlayer = new NativeArray<byte>(enemyCount, Allocator.Persistent);

		var rng = new Unity.Mathematics.Random(12345);
		for (int i = 0; i < enemyCount; i++)
		{
			float2 p = rng.NextFloat2Direction() * rng.NextFloat(0f, spawnRadius);
			posRead[i] = p;
			velRead[i] = rng.NextFloat2Direction() * 0.5f;
		}

		// Important: start with write buffers equal to read buffers
		posWrite.CopyFrom(posRead);
		velWrite.CopyFrom(velRead);

		quadMesh = BuildQuadMesh();
		matrices = new Matrix4x4[BatchSize];
	}

	void Update()
	{
		handle.Complete();

		grid.Clear();
		for (int i = 0; i < enemyCount; i++) hitPlayer[i] = 0;

		var buildGrid = new BuildGridJob
		{
			cellSize = cellSize,
			pos = posRead,                         // READ ONLY
			gridWriter = grid.AsParallelWriter()
		}.ScheduleParallel(enemyCount, 256, default);

		var sim = new EnemySimWithCollisionsJob
		{
			dt = Time.deltaTime,
			playerPos = new float2(player.position.x, player.position.y),
			speed = enemySpeed,
			steerStrength = steerStrength,

			enemyRadius = enemyRadius,
			playerRadius = playerRadius,
			collisionPush = collisionPush,
			cellSize = cellSize,

			posRead = posRead,                     // READ ONLY (can read j)
			velRead = velRead,                     // READ ONLY
			posWrite = posWrite,                   // WRITE i only
			velWrite = velWrite,                   // WRITE i only

			grid = grid,                           // READ ONLY
			hitPlayer = hitPlayer                  // WRITE i only
		}.ScheduleParallel(enemyCount, 256, buildGrid);

		handle = sim;
	}

	void LateUpdate()
	{
		if (!posRead.IsCreated) return;

		handle.Complete();

		// Render the freshly written state
		RenderEnemies(posWrite, velWrite);

		// Swap buffers AFTER rendering
		var tmpP = posRead; posRead = posWrite; posWrite = tmpP;
		var tmpV = velRead; velRead = velWrite; velWrite = tmpV;
	}

	void OnDisable()
	{
		// If the arrays were never created (e.g., disabled before OnEnable finished), exit safely.
		if (!posRead.IsCreated && !posWrite.IsCreated && !velRead.IsCreated && !velWrite.IsCreated)
			return;

		handle.Complete();

		if (posA.IsCreated) posA.Dispose();
		if (posB.IsCreated) posB.Dispose();
		if (velA.IsCreated) velA.Dispose();
		if (velB.IsCreated) velB.Dispose();

		if (grid.IsCreated) grid.Dispose();
		if (hitPlayer.IsCreated) hitPlayer.Dispose();
	}

	void RenderEnemies(NativeArray<float2> pArr, NativeArray<float2> vArr)
	{
		if (!enemyMaterial || !quadMesh) return;

		int remaining = enemyCount;
		int offset = 0;

		while (remaining > 0)
		{
			int count = Mathf.Min(remaining, BatchSize);

			for (int i = 0; i < count; i++)
			{
				float2 p = pArr[offset + i];
				float2 v = vArr[offset + i];

				float angleDeg = math.degrees(math.atan2(v.y, v.x)) - 90f;
				var rot = Quaternion.Euler(0f, 0f, angleDeg);
				matrices[i] = Matrix4x4.TRS(new Vector3(p.x, p.y, 0f), rot, new Vector3(enemyScale, enemyScale, 1f));
			}

			Graphics.DrawMeshInstanced(quadMesh, 0, enemyMaterial, matrices, count);

			offset += count;
			remaining -= count;
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
			int2 cell = (int2)math.floor(pos[i] / cellSize);
			gridWriter.Add(HashCell(cell), i);
		}
	}

	[BurstCompile]
	struct EnemySimWithCollisionsJob : IJobFor
	{
		public float dt;
		public float2 playerPos;
		public float speed;
		public float steerStrength;

		public float enemyRadius;
		public float playerRadius;
		public float collisionPush;
		public float cellSize;

		[ReadOnly] public NativeArray<float2> posRead;
		[ReadOnly] public NativeArray<float2> velRead;

		public NativeArray<float2> posWrite;
		public NativeArray<float2> velWrite;

		[ReadOnly] public NativeParallelMultiHashMap<int, int> grid;
		public NativeArray<byte> hitPlayer;

		public void Execute(int i)
		{
			float2 p = posRead[i];
			float2 v = velRead[i];

			// steer towards player
			float2 toPlayer = playerPos - p;
			float distSq = math.lengthsq(toPlayer);
			float2 desiredDir = distSq > 1e-6f ? math.normalize(toPlayer) : new float2(0, 1);
			float2 desiredVel = desiredDir * speed;
			v = math.lerp(v, desiredVel, math.saturate(steerStrength * dt));

			// collisions vs neighbors (read posRead[j], write only p/v for i)
			int2 myCell = (int2)math.floor(p / cellSize);
			float minDist = enemyRadius * 2f;
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
				v += math.normalize(push) * (0.5f * collisionPush * dt);
			}

			// collision vs player
			float minPlayerDist = enemyRadius + playerRadius;
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
		// Stable 2D hash (works for negatives)
		unchecked
		{
			int h = 17;
			h = h * 31 + c.x;
			h = h * 31 + c.y;
			return h;
		}
	}
}