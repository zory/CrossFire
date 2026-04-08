using Unity.Entities;

namespace Core.Physics
{
	public struct ConcaveTrianglesRef : IComponentData
	{
		public BlobAssetReference<TriangleSoupBlob> Value;
	}
}