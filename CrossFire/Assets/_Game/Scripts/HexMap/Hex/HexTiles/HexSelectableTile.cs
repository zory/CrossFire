using UnityEngine;

namespace CrossFire.HexMap
{
    // Component for the selectable tile prefab.
    // Owns the visual representation; exposes an API to control appearance.
    public class HexSelectableTile : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
    }
}