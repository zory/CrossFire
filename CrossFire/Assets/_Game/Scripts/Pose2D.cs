using System;
using Unity.Mathematics;

namespace CrossFire
{
	[Serializable]
	public struct Pose2D
	{
		public float2 Position;
		public float Theta;

		public override string ToString()
		{
			return string.Format("[{0}|{1}]", Position, Theta);
		}
	}
}