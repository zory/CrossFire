using UnityEngine;

namespace CrossFire.HexMap
{
	public interface IHexMapLayerSerializer
	{
		void Save(string fileName, HexMapModel model);
		void LoadInto(string fileName, HexMapModel model);
	}
}
