using System.IO;
using UnityEngine;

namespace CrossFire.Utilities
{
    public static class PersistentDataHelper
    {
		public static void SaveToFile(string relativePath, string content)
		{
			string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
			WriteAllTextSafe(fullPath, content, append: false);
		}

		public static void WriteAllTextSafe(string fullPath, string content, bool append)
		{
			string directory = Path.GetDirectoryName(fullPath);

			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			if (append)
			{
				File.AppendAllText(fullPath, content);
			}
			else
			{
				File.WriteAllText(fullPath, content);
			}
		}

		public static string LoadFromFile(string relativePath)
		{
			string path = Path.Combine(Application.streamingAssetsPath, relativePath);
			if (File.Exists(path))
			{
				return File.ReadAllText(path);
			}
			return string.Empty;
		}
	}
}
