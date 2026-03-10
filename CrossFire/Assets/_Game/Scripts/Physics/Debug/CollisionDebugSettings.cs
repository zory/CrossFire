using UnityEngine;

namespace CrossFire.Physics
{
	public static class CollisionDebugSettings
	{
#if UNITY_EDITOR
		private const string EnabledKey = "CrossFire.Physics.CollisionDebug.Enabled";
		private const string DrawBroadphaseKey = "CrossFire.Physics.CollisionDebug.DrawBroadphase";
		private const string DrawHitTrianglesKey = "CrossFire.Physics.CollisionDebug.DrawHitTriangles";


		private const string GridCellColorKey = "CrossFire.Physics.CollisionDebug.GridCellColor";
		private const string CircleShapeColorKey = "CrossFire.Physics.CollisionDebug.CircleShapeColor";
		private const string ConcaveShapeColorKey = "CrossFire.Physics.CollisionDebug.ConcaveShapeColor";
		private const string BoundRadiusColorKey = "CrossFire.Physics.CollisionDebug.BoundRadiusColor";
		private const string BroadphaseLinkColorKey = "CrossFire.Physics.CollisionDebug.BroadphaseLinkColor";
		private const string HitTriangleColorKey = "CrossFire.Physics.CollisionDebug.HitTriangleColor";
		private const string TrianglesPreviewColorKey = "CrossFire.Physics.CollisionDebug.TrianglesPreviewColorKey";

		public static bool Enabled => UnityEditor.EditorPrefs.GetBool(EnabledKey, false);
		public static bool DrawBroadphase => UnityEditor.EditorPrefs.GetBool(DrawBroadphaseKey, true);
		public static bool DrawHitTriangles => UnityEditor.EditorPrefs.GetBool(DrawHitTrianglesKey, true);

		public static Color GridCellColor => GetColor(GridCellColorKey, new Color(1f, 1f, 1f, 0.95f));
		public static Color CircleShapeColor => GetColor(CircleShapeColorKey, new Color(0f, 1f, 1f, 0.95f));
		public static Color ConcaveShapeColor => GetColor(ConcaveShapeColorKey, new Color(0f, 1f, 0f, 0.95f));
		public static Color BoundRadiusColor => GetColor(BoundRadiusColorKey, new Color(1f, 1f, 0f, 0.95f));
		public static Color BroadphaseLinkColor => GetColor(BroadphaseLinkColorKey, new Color(1f, 0.5f, 0f, 1f));
		public static Color HitTriangleColor => GetColor(HitTriangleColorKey, new Color(1f, 0f, 0f, 1f));
		public static Color TrianglesPreviewColor => GetColor(TrianglesPreviewColorKey, new Color(0f, 0.7f, 1f, 0.8f));

		public static void SetGridCellColor(Color value) => SetColor(GridCellColorKey, value);
		public static void SetCircleShapeColor(Color value) => SetColor(CircleShapeColorKey, value);
		public static void SetConcaveShapeColor(Color value) => SetColor(ConcaveShapeColorKey, value);
		public static void SetBoundRadiusColor(Color value) => SetColor(BoundRadiusColorKey, value);
		public static void SetBroadphaseLinkColor(Color value) => SetColor(BroadphaseLinkColorKey, value);
		public static void SetHitTriangleColor(Color value) => SetColor(HitTriangleColorKey, value);
		public static void SetTrianglesPreviewColor(Color value) => SetColor(TrianglesPreviewColorKey, value);

		private static Color GetColor(string key, Color fallback)
		{
			string defaultHtml = "#" + ColorUtility.ToHtmlStringRGBA(fallback);
			string html = UnityEditor.EditorPrefs.GetString(key, defaultHtml);

			if (ColorUtility.TryParseHtmlString(html, out Color color))
			{
				return color;
			}

			return fallback;
		}

		private static void SetColor(string key, Color value)
		{
			string html = "#" + ColorUtility.ToHtmlStringRGBA(value);
			UnityEditor.EditorPrefs.SetString(key, html);
		}
#else
		public static bool Enabled => false;
		public static bool DrawBroadphase => false;
		public static bool DrawHitTriangles => false;

		public static Color CircleShapeColor => new Color(0f, 1f, 1f, 0.95f);
		public static Color ConcaveShapeColor => new Color(0f, 1f, 0f, 0.95f);
		public static Color BoundRadiusColor => new Color(1f, 1f, 0f, 0.95f);
		public static Color BroadphaseLinkColor => new Color(1f, 0.5f, 0f, 1f);
		public static Color HitTriangleColor => new Color(1f, 0f, 0f, 1f);
		public static Color TrianglesPreviewColor => new Color(0f, 0.7f, 1f, 0.8f);
#endif

		public const int CircleSegments = 24;
		public const float ZOffset = 0f;
	}
}