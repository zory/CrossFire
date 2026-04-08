using NUnit.Framework;
using Unity.Mathematics;

namespace Core.Physics.Tests.EditMode
{
	/// <summary>
	/// Assertion helpers for physics values.
	/// Integration math accumulates floating-point error, so direct equality checks are
	/// unreliable. Use these instead of Assert.AreEqual when testing computed positions,
	/// velocities, or rotations.
	/// </summary>
	public static class PhysicsAssert
	{
		public const float DEFAULT_DELTA = 1e-5f;

		public static void AreEqual(float2 expected, float2 actual, float delta = DEFAULT_DELTA, string message = null)
		{
			string prefix = message != null ? message + " " : string.Empty;
			Assert.AreEqual(expected.x, actual.x, delta, $"{prefix}float2.x mismatch");
			Assert.AreEqual(expected.y, actual.y, delta, $"{prefix}float2.y mismatch");
		}

		public static void AreEqual(Pose2D expected, Pose2D actual, float delta = DEFAULT_DELTA, string message = null)
		{
			string prefix = message != null ? message + " " : string.Empty;
			AreEqual(expected.Position, actual.Position, delta, $"{prefix}Position");
			Assert.AreEqual(expected.ThetaRad, actual.ThetaRad, delta, $"{prefix}ThetaRad mismatch");
		}
	}
}
