using System;
using System.Diagnostics;
using System.IO;

namespace HeyClicky.App.Dock;

internal static class DockFiles
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
		if (text.StartsWith("file://", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(text, UriKind.Absolute, out Uri result))
		{
			text = result.LocalPath;
		}
		text = Environment.ExpandEnvironmentVariables(text.Replace('/', '\\'));
		if (text.StartsWith("~\\"))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string text2 = text;
			text = Path.Combine(folderPath, text2.Substring(2, text2.Length - 2));
		}
		if (Path.IsPathRooted(text))
		{
			return text;
		}
		if (workspace != null)
		{
			return Path.GetFullPath(Path.Combine(workspace, text));
		}
		return text;
	}

	public static void Open(string entry, string? workspace)
	{
		try
		{
			string text = Resolve(entry, workspace);
			if (!IsWebLink(entry) && !File.Exists(text) && !Directory.Exists(text))
			{
				string directoryName = Path.GetDirectoryName(text);
				while (directoryName != null && !Directory.Exists(directoryName))
				{
					directoryName = Path.GetDirectoryName(directoryName);
				}
				if (directoryName == null)
				{
					return;
				}
				text = directoryName;
			}
			ShellOpen.Start(text);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "open " + entry + ": " + ex.Message);
		}
	}

	public static void Reveal(string entry, string? workspace)
	{
		if (IsWebLink(entry))
		{
			return;
		}
		string text = Resolve(entry, workspace);
		try
		{
			if (File.Exists(text))
			{
				Process.Start("explorer.exe", "/select,\"" + text + "\"");
			}
			else
			{
				Open(entry, workspace);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "reveal " + entry + ": " + ex.Message);
		}
	}

	public static string FormatBytes(long b)
	{
		if (b < 1000000)
		{
			if (b < 1000)
			{
				return $"{b} bytes";
			}
			return $"{(double)b / 1000.0:0} KB";
		}
		if (b < 1000000000)
		{
			return $"{(double)b / 1000000.0:0.#} MB";
		}
		return $"{(double)b / 1000000000.0:0.#} GB";
	}
}
