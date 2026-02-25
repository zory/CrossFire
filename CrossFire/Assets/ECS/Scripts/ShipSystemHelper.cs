using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipSystemHelper
{
	public static float2 SamplePoint(List<SpawnAreaElement> areas, ref Unity.Mathematics.Random rng)
	{
		int idx = rng.NextInt(0, areas.Count);
		SpawnAreaElement area = areas[idx];

		float x = rng.NextFloat(area.Min.x, area.Max.x);
		float y = rng.NextFloat(area.Min.y, area.Max.y);
		return new float2(x, y);
	}

	public static bool IsFree(float2 position, float minDistSq, float cellSize, Dictionary<long, List<float2>> grid)
	{
		int2 cell = (int2)math.floor(position / cellSize);

		//Will look around
		for (int row = -1; row <= 1; row++)
		{
			for (int column = -1; column <= 1; column++)
			{
				int2 neighbouringCell = cell + new int2(column, row);
				long key = HashCell64(neighbouringCell);

				if (!grid.TryGetValue(key, out List<float2> list))
				{
					continue;
				}

				for (int i = 0; i < list.Count; i++)
				{
					float2 distance = position - list[i];
					if (math.lengthsq(distance) < minDistSq)
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	public static void AddToGrid(float2 position, float cellSize, Dictionary<long, List<float2>> grid)
	{
		int2 cell = (int2)math.floor(position / cellSize);
		long key = HashCell64(cell);

		if (!grid.TryGetValue(key, out List<float2> list))
		{
			list = new List<float2>();
			grid.Add(key, list);
		}

		list.Add(position);
	}

	public static long HashCell64(int2 cell)
	{
		unchecked { return ((long)cell.x << 32) ^ (uint)cell.y; }
	}
}
