//// Bullet-vs-ConcaveTargets collision for DOTS/ECS (2D).
//// - Concave ships/walls are represented as a TRIANGLE LIST (no holes assumed).
//// - Broadphase: uniform grid (spatial hash) of targets.
//// - Narrowphase: circle (bullet) vs triangles (target).
//// - Filtering: Layer/Mask bits (reusable for future collisions).
////
//// You can reuse the same grid + filter + narrowphase dispatch for other collision pairs later.

//using Unity.Burst;
//using Unity.Collections;
//using Unity.Mathematics;

using Unity.Entities;

namespace CrossFire.Physics
{
	#region Collision filtering (reusable)

	public static class CollisionFilterUtil
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static bool CanCollide(
			in CollisionLayer firstCollisionLayer,
			in CollisionMask firstCollisionMask,
			in CollisionLayer secondCollisionLayer,
			in CollisionMask secondCollisionMask)
		{
			bool firstCanCollideWithSecond =
				(firstCollisionMask.Value & secondCollisionLayer.Value) != 0u;

			bool secondCanCollideWithFirst =
				(secondCollisionMask.Value & firstCollisionLayer.Value) != 0u;

			if (!firstCanCollideWithSecond)
			{
				return false;
			}

			if (!secondCanCollideWithFirst)
			{
				return false;
			}

			return true;
		}
	}
	#endregion
}