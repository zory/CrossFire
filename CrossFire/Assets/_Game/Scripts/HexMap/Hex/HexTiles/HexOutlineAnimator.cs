using UnityEngine;

namespace CrossFire.HexMap
{
    // Animates the glowing hex outline prefab at runtime.
    // Add alongside GlowingHexOutlineUI on the root of the outline prefab.
    //
    // Three layered effects — each runs on its own independent period so they
    // drift in and out of phase, giving an organic rather than mechanical feel:
    //
    //   Alpha pulse  — all sprites breathe between alphaMin and alphaMax.
    //   Color shift  — all sprites slowly tint between colorA and colorB.
    //   Wall scale   — edge walls subtly widen and narrow on their local X axis.
    public class HexOutlineAnimator : MonoBehaviour
    {
        [SerializeField]
        private GlowingHexOutlineUI hexOutline;

        [Header("Alpha Pulse")]
        [SerializeField]
        private float pulseDuration = 1.5f;
        [SerializeField]
        [Range(0f, 1f)]
        private float alphaMin = 0.25f;
        [SerializeField]
        [Range(0f, 1f)]
        private float alphaMax = 1f;

        [Header("Color Shift")]
        [SerializeField]
        private Color colorA = Color.white;
        [SerializeField]
        private Color colorB = new Color(0.55f, 0.85f, 1f); // cool blue tint
        [SerializeField]
        private float colorDuration = 3.2f; // prime-ish so it drifts vs pulse

        [Header("Wall Scale")]
        [SerializeField]
        private float scaleDuration = 2.3f;
        [SerializeField]
        [Range(0f, 0.5f)]
        private float scaleAmplitude = 0.12f; // fraction of base X scale to add/subtract

        private SpriteRenderer[] _allRenderers;
        private Transform[]      _wallTransforms;
        private float[]          _wallBaseScaleX;

        private void Awake()
        {
            _allRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            if (hexOutline != null && hexOutline.EdgesGameObjects != null)
            {
                int wallCount = hexOutline.EdgesGameObjects.Length;
                _wallTransforms = new Transform[wallCount];
                _wallBaseScaleX = new float[wallCount];

                for (int i = 0; i < wallCount; i++)
                {
                    if (hexOutline.EdgesGameObjects[i] == null)
                    {
                        continue;
                    }

                    _wallTransforms[i] = hexOutline.EdgesGameObjects[i].transform;
                    _wallBaseScaleX[i] = _wallTransforms[i].localScale.x;
                }
            }
        }

        private void Update()
        {
            float t = Time.time;

            // Each effect uses a sine mapped to [0, 1].
            float pulseT = SineT(t, pulseDuration);
            float colorT = SineT(t, colorDuration);
            float scaleT = SineT(t, scaleDuration);

            // Combine: lerp color first, then override alpha with pulse.
            Color baseColor = Color.Lerp(colorA, colorB, colorT);
            baseColor.a = Mathf.Lerp(alphaMin, alphaMax, pulseT);

            for (int i = 0; i < _allRenderers.Length; i++)
            {
                _allRenderers[i].color = baseColor;
            }

            // Wall scale: widen and narrow around the base X scale.
            if (_wallTransforms != null)
            {
                for (int i = 0; i < _wallTransforms.Length; i++)
                {
                    if (_wallTransforms[i] == null)
                    {
                        continue;
                    }

                    Vector3 scale = _wallTransforms[i].localScale;
                    scale.x = _wallBaseScaleX[i] * Mathf.Lerp(1f - scaleAmplitude, 1f + scaleAmplitude, scaleT);
                    _wallTransforms[i].localScale = scale;
                }
            }
        }

        // Maps a sine wave to [0, 1] over the given period.
        private static float SineT(float time, float period)
        {
            return (Mathf.Sin(time * Mathf.PI * 2f / period) + 1f) * 0.5f;
        }
    }
}
