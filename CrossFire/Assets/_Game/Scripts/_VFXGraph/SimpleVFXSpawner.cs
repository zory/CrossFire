using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class SimpleVFXSpawner : MonoBehaviour
{
	[Serializable]
	public struct SerializableVFX
	{
		public Vector3 Position;
		Color Color;
	}

	public VisualEffect Vfx;
	public string PositionsBufferName = "SpawnBufferPositions";
	public string ColorsBufferName = "SpawnBufferColors";
	public string CountName = "SpawnCount";
	public string EventName = "OnSpawnBatch";

	public int InitialCapacity = 1024;

	private readonly List<Vector3> _positions = new();
	private readonly List<Vector4> _colors = new();

	private GraphicsBuffer _posBuffer;
	private GraphicsBuffer _colBuffer;
	private int _capacity;

	private void Awake()
	{
		if (Vfx == null)
			Vfx = GetComponent<VisualEffect>();

		EnsureCapacity(Mathf.Max(1, InitialCapacity));
	}

	private void OnDestroy()
	{
		ReleaseBuffers();
	}

	public void AddPoint(Vector3 worldPosition, Color color)
	{
		_positions.Add(worldPosition);
		_colors.Add((Vector4)color);
	}

	/// <summary>
	/// Uploads all queued points to the GPU and triggers one VFX event.
	/// Call this once per frame (or less often).
	/// </summary>
	public void Flush()
	{
		if (Vfx == null)
			return;

		int count = _positions.Count;
		if (count == 0)
			return;

		EnsureCapacity(count);

		// Upload data
		_posBuffer.SetData(_positions, 0, 0, count);
		_colBuffer.SetData(_colors, 0, 0, count);

		// Bind buffers + count
		Vfx.SetGraphicsBuffer(PositionsBufferName, _posBuffer);
		Vfx.SetGraphicsBuffer(ColorsBufferName, _colBuffer);
		Vfx.SetUInt(CountName, (uint)count);

		// Trigger one spawn event
		Vfx.SendEvent(EventName);

		_positions.Clear();
		_colors.Clear();
	}

	private void EnsureCapacity(int needed)
	{
		if (_posBuffer != null && _colBuffer != null && needed <= _capacity)
			return;

		ReleaseBuffers();

		_capacity = Mathf.NextPowerOfTwo(Mathf.Max(1, needed));

		// Structured buffers:
		// Vector3 -> 3 floats = 12 bytes
		// Vector4 -> 4 floats = 16 bytes
		_posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, sizeof(float) * 3);
		_colBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, sizeof(float) * 4);
	}

	private void ReleaseBuffers()
	{
		if (_posBuffer != null) { _posBuffer.Release(); _posBuffer = null; }
		if (_colBuffer != null) { _colBuffer.Release(); _colBuffer = null; }
		_capacity = 0;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			p.z = 0f;

			for (int i = 0; i < 20; i++)
			{
				AddPoint(p, i % 2 == 0 ? Color.red : Color.blue);
				p.x += 1;
			}
				
			Flush();
		}
	}
}