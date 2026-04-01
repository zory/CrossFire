using UnityEngine;

namespace CrossFire.HexMap
{
	public sealed class HexMapLoadPipeline
	{
		private readonly IHexMapLayerSerializer[] _serializers;

		public HexMapLoadPipeline(params IHexMapLayerSerializer[] serializers)
		{
			_serializers = serializers;
		}

		public void Save(string fileName, HexMapModel model)
		{
			for (int i = 0; i < _serializers.Length; i++)
			{
				_serializers[i].Save(fileName, model);
			}
		}

		public HexMapModel Load(string fileName)
		{
			HexMapModel model = new HexMapModel();
			for (int i = 0; i < _serializers.Length; i++)
			{
				_serializers[i].LoadInto(fileName, model);
			}

			HexMapModelOperations.ReindexTiles(model);
			return model;
		}
	}
}
