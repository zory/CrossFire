using UnityEngine;

namespace CrossFire.HexMap
{
    // Runtime editor tool for creating and modifying mission definitions.
    // Set MissionId, edit MissionData fields in the inspector, then tick SaveNow to persist.
    // Tick LoadNow to populate MissionData from the file for the given MissionId.
    public class MissionDataEditorTool : MonoBehaviour
    {
        [SerializeField]
        private int _missionId;

        [SerializeField]
        private MissionData _missionData;

        [SerializeField]
        private bool _saveNow;

        [SerializeField]
        private bool _loadNow;

        private void Update()
        {
            if (_saveNow)
            {
                _saveNow = false;
                Save();
            }

            if (_loadNow)
            {
                _loadNow = false;
                Load();
            }
        }

        private void Save()
        {
            _missionData.Id = _missionId;
            MissionDataSaveData.Save(_missionId, _missionData);
            Debug.Log($"[MissionDataEditorTool] Saved mission {_missionId}: \"{_missionData.Name}\"");
        }

        private void Load()
        {
            _missionData = MissionDataSaveData.Load(_missionId);
            Debug.Log($"[MissionDataEditorTool] Loaded mission {_missionId}: \"{_missionData.Name}\"");
        }
    }
}
