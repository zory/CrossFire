using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
	public interface IHexMapVisualLayer
	{
		void Apply(HexMapContext context, IReadOnlyDictionary<Vector3Int, HexCell> cellsByPosition);
	}
}
