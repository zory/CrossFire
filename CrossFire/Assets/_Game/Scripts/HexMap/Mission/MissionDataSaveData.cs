using CrossFire.Utilities;

namespace CrossFire.HexMap
{
    public static class MissionDataSaveData
    {
        [System.Serializable]
        public class MissionDataWrapper
        {
            public MissionData Mission;
        }

        public const string RELATIVE_MISSIONS_PATH = "Data/Missions/";
        public const string MISSIONS_EXTENSION = ".mission";

        public static void Save(int missionId, MissionData missionData)
        {
            MissionDataWrapper wrapper = new MissionDataWrapper
            {
                Mission = missionData
            };

            SaveDataFileHelper.SaveWrapper(
                RELATIVE_MISSIONS_PATH,
                MISSIONS_EXTENSION,
                missionId.ToString(),
                wrapper
            );
        }

        public static MissionData Load(int missionId)
        {
            MissionDataWrapper wrapper = SaveDataFileHelper.LoadWrapper(
                RELATIVE_MISSIONS_PATH,
                MISSIONS_EXTENSION,
                missionId.ToString(),
                () => new MissionDataWrapper()
            );

            return wrapper.Mission;
        }
    }
}
