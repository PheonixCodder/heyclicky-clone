using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.App.Notch;

public static class NotchPreferences
{
	private static readonly string FilePath = AppPaths.Combine("notch.json");

	private static JsonObject? data;

	public const bool IsProactiveAgentSuggestionsPausedForRelease = true;

	private static JsonObject Data
	{
		get
		{
			if (data != null)
			{
				return data;
			}
			try
			{
				data = (File.Exists(FilePath) ? (JsonNode.Parse(File.ReadAllText(FilePath), null) as JsonObject) : null);
			}
			catch (Exception ex)
			{
				Log.Warn("notch", "notch.json unreadable: " + ex.Message);
			}
			if (data == null)
			{
				data = new JsonObject((JsonNodeOptions?)null);
			}
			if (data["hasClickedHomeReferralButton"] == null)
			{
				data["hasClickedHomeReferralButton"] = ReadLegacyHomeFlag();
			}
			return data;
		}
	}

	public static bool HasClickedHomeReferralButton
	{
		get
		{
			return Data["hasClickedHomeReferralButton"]?.GetValue<bool>() ?? false;
		}
		set
		{
			Data["hasClickedHomeReferralButton"] = value;
			Save();
		}
	}

	private static bool ReadLegacyHomeFlag()
	{
		try
		{
			string path = AppPaths.Combine("home.json");
			int result;
			if (File.Exists(path))
			{
				JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(path), null);
				result = ((jsonNode != null && jsonNode["HasClickedReferralButton"]?.GetValue<bool>() == true) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
		catch
		{
			return false;
		}
	}

	private static void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			File.WriteAllText(FilePath + ".tmp", Data.ToJsonString(new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			File.Move(FilePath + ".tmp", FilePath, overwrite: true);
		}
		catch (Exception ex)
		{
			Log.Warn("notch", "notch.json write failed: " + ex.Message);
		}
	}
}
