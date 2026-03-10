using UnityEditor;

namespace CrossFire.Physics
{
	public static class CollisionDebugMenu
	{
		private const string Root = "CrossFire/Collision/";
		private const string EnabledPath = Root + "Enabled";
		private const string DrawBroadphasePath = Root + "Draw Broadphase";
		private const string DrawHitTrianglesPath = Root + "Draw Hit Triangles";

		private const string EnabledKey = "CrossFire.Physics.CollisionDebug.Enabled";
		private const string DrawBroadphaseKey = "CrossFire.Physics.CollisionDebug.DrawBroadphase";
		private const string DrawHitTrianglesKey = "CrossFire.Physics.CollisionDebug.DrawHitTriangles";

		[InitializeOnLoadMethod]
		private static void Init()
		{
			SyncChecks();
		}

		[MenuItem(EnabledPath)]
		private static void ToggleEnabled()
		{
			bool value = !EditorPrefs.GetBool(EnabledKey, false);
			EditorPrefs.SetBool(EnabledKey, value);
			SyncChecks();
		}

		[MenuItem(EnabledPath, true)]
		private static bool ToggleEnabledValidate()
		{
			Menu.SetChecked(EnabledPath, EditorPrefs.GetBool(EnabledKey, false));
			return true;
		}

		[MenuItem(DrawBroadphasePath)]
		private static void ToggleDrawBroadphase()
		{
			bool value = !EditorPrefs.GetBool(DrawBroadphaseKey, true);
			EditorPrefs.SetBool(DrawBroadphaseKey, value);
			SyncChecks();
		}

		[MenuItem(DrawBroadphasePath, true)]
		private static bool ToggleDrawBroadphaseValidate()
		{
			Menu.SetChecked(DrawBroadphasePath, EditorPrefs.GetBool(DrawBroadphaseKey, true));
			return true;
		}

		[MenuItem(DrawHitTrianglesPath)]
		private static void ToggleDrawHitTriangles()
		{
			bool value = !EditorPrefs.GetBool(DrawHitTrianglesKey, true);
			EditorPrefs.SetBool(DrawHitTrianglesKey, value);
			SyncChecks();
		}

		[MenuItem(DrawHitTrianglesPath, true)]
		private static bool ToggleDrawHitTrianglesValidate()
		{
			Menu.SetChecked(DrawHitTrianglesPath, EditorPrefs.GetBool(DrawHitTrianglesKey, true));
			return true;
		}

		private static void SyncChecks()
		{
			Menu.SetChecked(EnabledPath, EditorPrefs.GetBool(EnabledKey, false));
			Menu.SetChecked(DrawBroadphasePath, EditorPrefs.GetBool(DrawBroadphaseKey, true));
			Menu.SetChecked(DrawHitTrianglesPath, EditorPrefs.GetBool(DrawHitTrianglesKey, true));
		}
	}
}