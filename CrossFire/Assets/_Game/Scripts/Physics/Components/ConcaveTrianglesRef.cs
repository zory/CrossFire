using Unity.Entities;

namespace CrossFire.Physics
{
	public struct ConcaveTrianglesRef : IComponentData
	{
		public BlobAssetReference<TriangleSoupBlob> Value;
	}
}