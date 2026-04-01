using System.Collections.Generic;
using UnityEngine;

namespace CrossFire.HexMap
{
    public class HexMapCreator_Teams : MonoBehaviour
    {
        public HexMapCreator_Base HexMapCreatorBase;

		public Color[] TeamColors;
        public int TeamId = 0;
        public bool EnableTeamCreator = true;

		public bool SaveNow;
		public bool LoadNow;

		private Dictionary<Vector3Int, int> _tilePositionAndTeamIdDict = new Dictionary<Vector3Int, int>();

		private void Start()
		{
			if (HexMapCreatorBase != null)
			{
				HexMapCreatorBase.OnMapCreated += UpdateMapTeams;
			}
		}

		private void OnDestroy()
		{
			if (HexMapCreatorBase != null)
			{
				HexMapCreatorBase.OnMapCreated -= UpdateMapTeams;
			}
		}

		private void Update()
		{
		    if (!EnableTeamCreator)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
				if (HexMapCreatorBase.TilePositionAndIndexDict.ContainsKey(HexMapCreatorBase.TileCoords))
                {
					_tilePositionAndTeamIdDict[HexMapCreatorBase.TileCoords] = TeamId;

					UpdateMapTeams();
				}
			}

			if (SaveNow)
			{
				SaveNow = false;
				WorldMapSaveData.SaveWorldMap(HexMapCreatorBase.FileName, _tilePositionAndTeamIdDict);
			}
			if (LoadNow)
			{
				LoadNow = false;

				ClearMapTeams();
				_tilePositionAndTeamIdDict.Clear();
				_tilePositionAndTeamIdDict = WorldMapSaveData.LoadWorldMap(HexMapCreatorBase.FileName);

				UpdateMapTeams();
			}
		}

		private void ClearMapTeams()
		{

		}

		private void UpdateMapTeams()
		{
			foreach (var tilePosAndTeamId in _tilePositionAndTeamIdDict)
			{
				if (HexMapCreatorBase.TilePositionAndGODict.TryGetValue(HexMapCreatorBase.TileCoords, out HexCell hexCell))
				{
					Color teamColor = Color.white;
					if (TeamId >= 0 && TeamId < TeamColors.Length)
					{
						teamColor = TeamColors[TeamId];
					}
					hexCell.SpriteRenderer.color = teamColor;
				}
			}
		}
	}
}
