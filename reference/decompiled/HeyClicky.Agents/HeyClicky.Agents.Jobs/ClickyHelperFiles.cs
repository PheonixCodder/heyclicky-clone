using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HeyClicky.Agents.Jobs;

public static class ClickyHelperFiles
{
	private static readonly object Gate = new object();

	private static readonly Dictionary<string, string> LastWrittenTextByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static string FolderPath { get; set; } = AppPaths.Combine("helper");

	public static string? WriteNoteGuide(string guideText)
	{
		return Write(guideText, "note-guide.md");
	}

	public static string? WriteUserProfile(string? profileMarkdown)
	{
		if (!string.IsNullOrWhiteSpace(profileMarkdown))
		{
			return Write(profileMarkdown, "about-the-user.md");
		}
		return null;
	}

	public static void RemoveAllForSignOut()
	{
		lock (Gate)
		{
			try
			{
				if (Directory.Exists(FolderPath))
				{
					Directory.Delete(FolderPath, recursive: true);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", "helper files not removed: " + ex.Message);
			}
			LastWrittenTextByPath.Clear();
		}
	}

	private static string? Write(string text, string fileName)
	{
		string text2 = Path.Combine(FolderPath, fileName);
		lock (Gate)
		{
			if (LastWrittenTextByPath.TryGetValue(text2, out string value) && value == text && File.Exists(text2))
			{
				return text2;
			}
			try
			{
				Directory.CreateDirectory(FolderPath);
				string text3 = text2 + ".tmp";
				File.WriteAllText(text3, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				File.Move(text3, text2, overwrite: true);
				LastWrittenTextByPath[text2] = text;
				return text2;
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", "couldn't write " + fileName + ": " + ex.Message);
				return null;
			}
		}
	}
}
