namespace CrossFire.Physics
{
	public static class CollisionDebugSettings
	{
#if UNITY_EDITOR
		private const string EnabledKey = "CrossFire.Physics.CollisionDebug.Enabled";
		private const string DrawBroadphaseKey = "CrossFire.Physics.CollisionDebug.DrawBroadphase";
		private const string DrawHitTrianglesKey = "CrossFire.Physics.CollisionDebug.DrawHitTriangles";

		public static bool Enabled => UnityEditor.EditorPrefs.GetBool(EnabledKey, false);
		public static bool DrawBroadphase => UnityEditor.EditorPrefs.GetBool(DrawBroadphaseKey, true);
		public static bool DrawHitTriangles => UnityEditor.EditorPrefs.GetBool(DrawHitTrianglesKey, true);
#else
		public static bool Enabled => false;
		public static bool DrawBroadphase => false;
		public static bool DrawHitTriangles => false;
#endif

		public const int CircleSegments = 24;
		public const float ZOffset = 0f;
	}
}