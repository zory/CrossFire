using System.IO;
using CrossFire.App;
using NUnit.Framework;
using UnityEngine;

namespace CrossFire.Tests.EditMode
{
	/// <summary>
	/// Tests for <see cref="MissionSaveData"/> — round-trip save/load and read-modify-write isolation.
	/// Uses mission ID 99999 to avoid colliding with real save files; the file is deleted in TearDown.
	/// </summary>
	public class MissionSaveDataTests
	{
		private const int TEST_MISSION_ID = 99999;

		[TearDown]
		public void TearDown()
		{
			string path = Path.Combine(
				Application.streamingAssetsPath,
				MissionSaveData.RELATIVE_PATH + TEST_MISSION_ID + MissionSaveData.EXTENSION
			);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		// ─── Metadata ─────────────────────────────────────────────────────────

		[Test]
		public void SaveMetadata_LoadMetadata_RoundTrips()
		{
			MissionData metadata = new MissionData
			{
				Id          = TEST_MISSION_ID,
				Name        = "Test Mission",
				Description = "A mission for automated testing.",
			};

			MissionSaveData.SaveMetadata(TEST_MISSION_ID, metadata);
			MissionData loaded = MissionSaveData.LoadMetadata(TEST_MISSION_ID);

			Assert.AreEqual(metadata.Id,          loaded.Id);
			Assert.AreEqual(metadata.Name,        loaded.Name);
			Assert.AreEqual(metadata.Description, loaded.Description);
		}

		[Test]
		public void LoadMetadata_WhenFileDoesNotExist_ReturnsDefaultStruct()
		{
			MissionData loaded = MissionSaveData.LoadMetadata(TEST_MISSION_ID);

			// Default struct — Id is 0, Name and Description are null/empty.
			Assert.AreEqual(0, loaded.Id);
		}

		// ─── Simulation ───────────────────────────────────────────────────────

		[Test]
		public void LoadSimulation_WhenNotSaved_ReturnsNull()
		{
			GameplaySimulationSnapshot result = MissionSaveData.LoadSimulation(TEST_MISSION_ID);

			Assert.IsNull(result);
		}

		[Test]
		public void SaveSimulation_LoadSimulation_RoundTrips()
		{
			GameplaySimulationSnapshot snapshot = new GameplaySimulationSnapshot
			{
				NextStableId = 7,
				Ships = new ShipSaveData[]
				{
					new ShipSaveData
					{
						StableId  = 1,
						ShipType  = 2,
						Team      = 0,
						PositionX = 10f,
						PositionY = -5f,
						ThetaRad  = 1.2f,
						Health    = 100,
					}
				},
				Bullets = new BulletSaveData[]
				{
					new BulletSaveData
					{
						BulletType        = 0,
						Team              = 0,
						OwnerStableId     = 1,
						PositionX         = 11f,
						PositionY         = -4f,
						LifetimeRemaining = 2.5f,
						BulletDamage      = 10,
					}
				},
			};

			MissionSaveData.SaveSimulation(TEST_MISSION_ID, snapshot);
			GameplaySimulationSnapshot loaded = MissionSaveData.LoadSimulation(TEST_MISSION_ID);

			Assert.IsNotNull(loaded);
			Assert.AreEqual(snapshot.NextStableId, loaded.NextStableId);

			Assert.AreEqual(1, loaded.Ships.Length);
			Assert.AreEqual(snapshot.Ships[0].StableId,  loaded.Ships[0].StableId);
			Assert.AreEqual(snapshot.Ships[0].ShipType,  loaded.Ships[0].ShipType);
			Assert.AreEqual(snapshot.Ships[0].PositionX, loaded.Ships[0].PositionX, 0.0001f);
			Assert.AreEqual(snapshot.Ships[0].PositionY, loaded.Ships[0].PositionY, 0.0001f);
			Assert.AreEqual(snapshot.Ships[0].ThetaRad,  loaded.Ships[0].ThetaRad,  0.0001f);
			Assert.AreEqual(snapshot.Ships[0].Health,    loaded.Ships[0].Health);

			Assert.AreEqual(1, loaded.Bullets.Length);
			Assert.AreEqual(snapshot.Bullets[0].OwnerStableId,     loaded.Bullets[0].OwnerStableId);
			Assert.AreEqual(snapshot.Bullets[0].LifetimeRemaining, loaded.Bullets[0].LifetimeRemaining, 0.0001f);
			Assert.AreEqual(snapshot.Bullets[0].BulletDamage,      loaded.Bullets[0].BulletDamage);
		}

		// ─── Read-modify-write isolation ──────────────────────────────────────

		[Test]
		public void SaveMetadata_PreservesExistingSimulation()
		{
			GameplaySimulationSnapshot snapshot = new GameplaySimulationSnapshot
			{
				NextStableId = 3,
				Ships        = new ShipSaveData[0],
				Bullets      = new BulletSaveData[0],
			};
			MissionSaveData.SaveSimulation(TEST_MISSION_ID, snapshot);

			MissionData metadata = new MissionData { Id = TEST_MISSION_ID, Name = "Updated" };
			MissionSaveData.SaveMetadata(TEST_MISSION_ID, metadata);

			GameplaySimulationSnapshot loadedSim = MissionSaveData.LoadSimulation(TEST_MISSION_ID);
			Assert.IsNotNull(loadedSim, "Simulation must still exist after saving metadata");
			Assert.AreEqual(3, loadedSim.NextStableId);
		}

		[Test]
		public void SaveSimulation_PreservesExistingMetadata()
		{
			MissionData metadata = new MissionData
			{
				Id          = TEST_MISSION_ID,
				Name        = "Keep Me",
				Description = "Should survive simulation save.",
			};
			MissionSaveData.SaveMetadata(TEST_MISSION_ID, metadata);

			GameplaySimulationSnapshot snapshot = new GameplaySimulationSnapshot
			{
				NextStableId = 5,
				Ships        = new ShipSaveData[0],
				Bullets      = new BulletSaveData[0],
			};
			MissionSaveData.SaveSimulation(TEST_MISSION_ID, snapshot);

			MissionData loadedMeta = MissionSaveData.LoadMetadata(TEST_MISSION_ID);
			Assert.AreEqual(metadata.Name,        loadedMeta.Name);
			Assert.AreEqual(metadata.Description, loadedMeta.Description);
		}
	}
}
