using System;

namespace CrossFire.App
{
	/// <summary>
	/// Metadata for a mission: persistent identifier, display name, and description.
	/// The full mission file (saved as <c>.mission</c>) contains this alongside the
	/// gameplay simulation snapshot. Use <see cref="MissionSaveData"/> to read and write it.
	/// </summary>
	[Serializable]
	public struct MissionData
	{
		public int    Id;
		public string Name;
		public string Description;
	}
}
