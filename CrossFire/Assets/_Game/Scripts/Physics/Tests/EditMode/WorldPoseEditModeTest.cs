using Core.Physics;
using NUnit.Framework;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	public class WorldPoseEditModeTest
	{
		[Test]
		public void WorldPose_DefaultValue_HasZeroPositionAndRotation()
		{
			WorldPose pose = new WorldPose();

			Assert.AreEqual(float2.zero, pose.Value.Position);
			Assert.AreEqual(0f, pose.Value.ThetaRad);
		}
	}
}
