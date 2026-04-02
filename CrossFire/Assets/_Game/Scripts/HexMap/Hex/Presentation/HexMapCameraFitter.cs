using UnityEngine;

namespace CrossFire.HexMap
{
    // Fits an orthographic camera to the current map.
    //
    // Assumptions:
    // - Tile pivots are centered.
    // - Tile size is constant.
    // - Extra padding in orthographic size covers the center-to-edge tile extent.
    //
    // Behavior:
    // - Centering is computed from tile center positions.
    // - Orthographic size is computed from projected tile center extents.
    // - Camera depth is computed explicitly so the map stays behind the near clip plane,
    //   instead of preserving whatever old depth the camera happened to have.
    //
    // positionOffset:
    //   x = camera local right
    //   y = camera local up
    //   z = extra backward distance along camera forward
    //
    // sizeOffset:
    //   flat padding added to orthographic size.
    public class HexMapCameraFitter : MonoBehaviour
    {
        [SerializeField]
        private HexMapController mapController;

        [SerializeField]
        private Camera targetCamera;

        [Header("Adjustment")]
        [SerializeField]
        private Vector3 positionOffset = Vector3.zero;

        [SerializeField]
        private float sizeOffset = 0.5f;

        [Header("Depth")]
        [SerializeField]
        private float nearClipPadding = 1f;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (mapController != null)
            {
                mapController.OnMapUpdated += FitCamera;
            }
        }

        private void OnDestroy()
        {
            if (mapController != null)
            {
                mapController.OnMapUpdated -= FitCamera;
            }
        }

        private void FitCamera()
        {
            if (targetCamera == null || mapController == null || mapController.CellsByPosition == null || mapController.CellsByPosition.Count == 0)
            {
                return;
            }

            Transform camTransform = targetCamera.transform;
            Vector3 camRight = camTransform.right;
            Vector3 camUp = camTransform.up;
            Vector3 camForward = camTransform.forward;

            Vector3 worldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 worldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            float minRight = float.MaxValue;
            float maxRight = float.MinValue;
            float minUp = float.MaxValue;
            float maxUp = float.MinValue;

            // Smallest forward projection = map point closest to the camera plane direction.
            // We use this to push the camera back enough to avoid near-plane clipping.
            float minForward = float.MaxValue;

            foreach (HexTile tile in mapController.CellsByPosition.Values)
            {
                Vector3 p = tile.transform.position;

                worldMin = Vector3.Min(worldMin, p);
                worldMax = Vector3.Max(worldMax, p);

                float r = Vector3.Dot(p, camRight);
                float u = Vector3.Dot(p, camUp);
                float f = Vector3.Dot(p, camForward);

                if (r < minRight) minRight = r;
                if (r > maxRight) maxRight = r;

                if (u < minUp) minUp = u;
                if (u > maxUp) maxUp = u;

                if (f < minForward) minForward = f;
            }

            // Center from world-space bounds of tile centers.
            Vector3 worldCenter = (worldMin + worldMax) * 0.5f;

            // Keep camera looking through the center in local right/up.
            float centerRight = Vector3.Dot(worldCenter, camRight);
            float centerUp = Vector3.Dot(worldCenter, camUp);

            // Place camera backward enough so nearest map point is behind near clip plane.
            // For any point P visible to the camera:
            // Dot(P - C, camForward) >= nearClipPlane
            // => Dot(C, camForward) <= Dot(P, camForward) - nearClipPlane
            // Use the nearest point and subtract padding too.
            float safeCameraForward = minForward - targetCamera.nearClipPlane - nearClipPadding;

            Vector3 newPosition =
                camRight * (centerRight + positionOffset.x) +
                camUp * (centerUp + positionOffset.y) +
                camForward * (safeCameraForward - positionOffset.z);

            camTransform.position = newPosition;

            // Orthographic size = half visible height.
            // Width must also fit after aspect correction.
            float halfHeight = (maxUp - minUp) * 0.5f;
            float halfWidth = (maxRight - minRight) * 0.5f;

            float requiredSize = Mathf.Max(halfHeight, halfWidth / targetCamera.aspect);
            targetCamera.orthographicSize = requiredSize + sizeOffset;
        }
    }
}