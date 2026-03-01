using UnityEngine;
using UnityEngine.UI;

public class OffscreenArrowItem : MonoBehaviour
{
	[SerializeField] private RectTransform arrowRect;
	[SerializeField] private Image arrowImage;

	private Camera worldCamera;
	private RectTransform boundsRect;
	private Camera uiCamera;

	// Arrow art points UP. If your sprite is different, change to +90 or 0.
	private const float AngleOffset = -90f;

	void Reset()
	{
		arrowRect = GetComponent<RectTransform>();
		arrowImage = GetComponent<Image>();
	}

	public void Init(Camera worldCam, RectTransform bounds)
	{
		worldCamera = worldCam;
		boundsRect = bounds;

		var canvas = boundsRect.GetComponentInParent<Canvas>();
		uiCamera = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
	}

	public void UpdateArrow(Vector3 targetWorldPosition)
	{
		if (!worldCamera || !boundsRect || !arrowRect) return;

		// World -> screen
		Vector2 targetScreen = worldCamera.WorldToScreenPoint(targetWorldPosition);

		// Screen -> bounds local
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			boundsRect, targetScreen, uiCamera, out Vector2 targetLocal
		);

		Rect r = boundsRect.rect;

		// Inside => hide
		if (r.Contains(targetLocal))
		{
			SetVisible(false);
			return;
		}
		SetVisible(true);

		Vector2 center = r.center;
		Vector2 dir = targetLocal - center;
		if (dir.sqrMagnitude < 0.0001f)
		{
			SetVisible(false);
			return;
		}

		Vector2 edgeLocal = RayRectEdgeIntersection(r, center, dir);

		// Position
		arrowRect.position = boundsRect.TransformPoint(edgeLocal);

		// Rotation from center -> target
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + AngleOffset;
		arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);
	}

	private void SetVisible(bool v)
	{
		if (arrowImage) arrowImage.enabled = v;
		else gameObject.SetActive(v);
	}

	static Vector2 RayRectEdgeIntersection(Rect rect, Vector2 origin, Vector2 dir)
	{
		float tMin = float.PositiveInfinity;

		if (Mathf.Abs(dir.x) > 1e-6f)
		{
			float tLeft = (rect.xMin - origin.x) / dir.x;
			float yLeft = origin.y + tLeft * dir.y;
			if (tLeft > 0f && yLeft >= rect.yMin && yLeft <= rect.yMax) tMin = Mathf.Min(tMin, tLeft);

			float tRight = (rect.xMax - origin.x) / dir.x;
			float yRight = origin.y + tRight * dir.y;
			if (tRight > 0f && yRight >= rect.yMin && yRight <= rect.yMax) tMin = Mathf.Min(tMin, tRight);
		}

		if (Mathf.Abs(dir.y) > 1e-6f)
		{
			float tBottom = (rect.yMin - origin.y) / dir.y;
			float xBottom = origin.x + tBottom * dir.x;
			if (tBottom > 0f && xBottom >= rect.xMin && xBottom <= rect.xMax) tMin = Mathf.Min(tMin, tBottom);

			float tTop = (rect.yMax - origin.y) / dir.y;
			float xTop = origin.x + tTop * dir.x;
			if (tTop > 0f && xTop >= rect.xMin && xTop <= rect.xMax) tMin = Mathf.Min(tMin, tTop);
		}

		if (float.IsInfinity(tMin))
			return origin; // should not happen if origin inside and dir != 0

		Vector2 hit = origin + dir * tMin;
		hit.x = Mathf.Clamp(hit.x, rect.xMin, rect.xMax);
		hit.y = Mathf.Clamp(hit.y, rect.yMin, rect.yMax);
		return hit;
	}
}