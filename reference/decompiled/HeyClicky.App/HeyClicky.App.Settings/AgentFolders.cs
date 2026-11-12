using System;

namespace HeyClicky.App.Settings;

public static class AgentFolders
{
	public static string Default => AppPaths.Combine("Clickys");

	public static string LegacyDocuments => AppPaths.DocumentsRoot;

	public static string Abbreviate(string path)
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		int length = folderPath.Length;
		return "~" + path.Substring(length, path.Length - length);
	}
}
