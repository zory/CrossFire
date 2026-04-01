using UnityEngine;
using UnityEngine.Serialization;

namespace CrossFire.HexMap
{
    public class HexMapLoader : MonoBehaviour
    {
        [FormerlySerializedAs("mapGameBootstrap")] [SerializeField]
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
        
        public void Save()
        {
            if (mapBootstrap == null)
            {
                return;
            }

            mapBootstrap.Save(fileName);
        }

        public void Load()
        {
            if (mapBootstrap == null)
            {
                return;
            }

            mapBootstrap.Load(fileName);
        }
    }
}
