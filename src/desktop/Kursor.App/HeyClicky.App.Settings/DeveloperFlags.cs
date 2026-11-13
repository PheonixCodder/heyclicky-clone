using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using HeyClicky.Core.Configuration;

namespace HeyClicky.App.Settings;

public static class DeveloperFlags
{
	private static readonly string FilePath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "developer-flags.json");

	private static readonly Lock Gate = new Lock();

	private static JsonObject? data;

	public static bool FullAgentActivity
	{
		get
		{
			return Read("full_agent_activity_debug");
		}
		set
		{
			Write("full_agent_activity_debug", value);
		}
	}

	public static event Action? Changed;

	private static bool Read(string key = "full_agent_activity_debug")
	{
		using (Gate.EnterScope())
		{
			if (data == null)
			{
				data = Load();
			}
			bool value = default(bool);
			return data[key] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value;
		}
	}

	private static void Write(string key, bool value)
	{
		using (Gate.EnterScope())
		{
			if (data == null)
			{
				data = Load();
			}
			data[key] = value;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				File.WriteAllText(FilePath + ".tmp", data.ToJsonString());
				File.Move(FilePath + ".tmp", FilePath, overwrite: true);
			}
			catch (Exception ex)
			{
				Log.Warn("settings", "developer-flags.json write failed: " + ex.Message);
			}
		}
		DeveloperFlags.Changed?.Invoke();
	}

	private static JsonObject Load()
	{
		try
		{
			if (File.Exists(FilePath) && JsonNode.Parse(File.ReadAllText(FilePath), null) is JsonObject result)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "developer-flags.json unreadable: " + ex.Message);
		}
		return new JsonObject((JsonNodeOptions?)null);
	}
}
