using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;

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

		public static void AreEqual(float3 expected, float3 actual, float delta = DEFAULT_DELTA, string message = null)
		{
			string prefix = message != null ? message + " " : string.Empty;
			Assert.AreEqual(expected.x, actual.x, delta, $"{prefix}float3.x mismatch");
			Assert.AreEqual(expected.y, actual.y, delta, $"{prefix}float3.y mismatch");
			Assert.AreEqual(expected.z, actual.z, delta, $"{prefix}float3.z mismatch");
		}

		/// <summary>
		/// Compares two quaternions as rotations. Uses |dot| ≈ 1 because q and -q represent
		/// the same rotation but have opposite component signs.
		/// </summary>
		public static void AreEqual(quaternion expected, quaternion actual, float delta = DEFAULT_DELTA, string message = null)
		{
			float dot = math.abs(math.dot(expected, actual));
			Assert.AreEqual(1f, dot, delta, message ?? "quaternion rotation mismatch");
		}
	}
}
