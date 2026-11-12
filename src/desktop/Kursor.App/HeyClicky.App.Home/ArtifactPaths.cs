using System;
using System.IO;

namespace HeyClicky.App.Home;

public static class ArtifactPaths
{
	public static bool IsWebLink(string entry)
	{
		if (!entry.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return entry.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static string Resolve(string entry, string? workspace)
	{
		if (IsWebLink(entry))
		{
			return entry;
		}
		string text = entry.Trim();
		if (text.StartsWith("file://", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(text, UriKind.Absolute, out Uri result) && result.IsFile)
		{
			text = result.LocalPath;
		}
		if (text.StartsWith('~'))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string text2 = text;
			text = Path.Combine(folderPath, text2.Substring(1, text2.Length - 1).TrimStart('/', '\\'));
		}
		if (Path.IsPathRooted(text))
		{
			return Path.GetFullPath(text);
		}
		if (workspace != null)
		{
			return Path.GetFullPath(Path.Combine(workspace, text));
		}
		return text;
	}

	public static bool SupportsInline(string path)
	{
		bool flag = !IsWebLink(path) && File.Exists(path);
		if (flag)
		{
			bool flag2;
			switch (Path.GetExtension(path).ToLowerInvariant())
			{
			case ".html":
			case ".htm":
			case ".xhtml":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = !flag2;
		}
		return flag;
	}

	public static void Reveal(string path)
	{
		if (IsWebLink(path))
		{
			return;
		}
		if (File.Exists(path) || Directory.Exists(path))
		{
			ChatBlockViews.RevealPath(path);
			return;
		}
		string directoryName = Path.GetDirectoryName(path);
		while (directoryName != null && !Directory.Exists(directoryName))
		{
			directoryName = Path.GetDirectoryName(directoryName);
		}
		if (directoryName != null)
		{
			ChatBlockViews.OpenPath(directoryName);
		}
	}
}
