using System.IO;
using HeyClicky.Core.Settings;

namespace HeyClicky.Core.Ink;

public static class ClickyLiveNotePreferences
{
	public const string FileName = "note.md";

	public const string EnabledDefaultsKey = "ClickyLiveNotesEnabled";

	public const string StreamsPatchesDefaultsKey = "ClickyLiveNotesStreamPatches";

	public static SettingsStore? Settings { get; set; }

	public static bool IsEnabled => false;

	public static bool StreamsPatches
	{
		get
		{
			if (IsEnabled)
			{
				return Settings?.Read((AppSettings s) => s.LiveNotesStreamPatches) ?? true;
			}
			return false;
		}
	}

	public static string NotePath(string folderPath)
	{
		return Path.GetFullPath(Path.Combine(folderPath, "note.md"));
	}
}
