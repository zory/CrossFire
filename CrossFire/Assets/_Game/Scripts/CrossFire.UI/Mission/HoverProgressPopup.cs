using FactorialFun.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CrossFire.App.UI
{
    // Radial fill indicator shown near the cursor while a hover charge is building up.
    // Set Image fill method to Radial360 on the prefab.
    public class HoverProgressPopup : PopupBase
    {
        [SerializeField]
        private Image _fillImage;

        public void SetProgress(float progress)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = Mathf.Clamp01(progress);
            }
        }
    }
}
