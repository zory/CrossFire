using UnityEngine;

namespace CrossFire.HexMap
{
    public class HexMapLoader : MonoBehaviour
    {
        [SerializeField]
        private HexMapBootstrap mapBootstrap;

        [SerializeField]
        private string fileName;
        [SerializeField]
        private bool saveNow;
        [SerializeField]
        private bool loadNow;
        
        private void Update()
        {
            if (saveNow)
            {
                saveNow = false;
                Save();
            }

            if (loadNow)
            {
                loadNow = false;
                Load();
            }
        }
        
        private void Save()
        {
            if (mapBootstrap == null)
            {
                return;
            }

            mapBootstrap.Save(fileName);
        }

        private void Load()
        {
            if (mapBootstrap == null)
            {
                return;
            }

            mapBootstrap.Load(fileName);
        }
    }
}
