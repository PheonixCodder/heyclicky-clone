using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.Agents.Backend;
using HeyClicky.App.Interop;
using HeyClicky.Core.Settings;

namespace HeyClicky.App.Settings;

public static class ObsidianVault
{
	private static SettingsStore? store;

	public static string? PersistedPath
	{
		get
		{
			string text = store?.Current.ObsidianVaultPath;
			if (text == null || text.Length <= 0)
			{
				return null;
			}
			return text;
		}
	}

	public static string? ConfiguredPath
	{
		get
		{
			string persistedPath = PersistedPath;
			if (persistedPath == null || !Directory.Exists(persistedPath))
			{
				return null;
			}
			return persistedPath;
		}
	}

	public static event Action? Changed;

	public static void Initialize(SettingsStore settings)
	{
		store = settings;
	}

	public static AgentIntegration Row()
	{
		string configuredPath = ConfiguredPath;
		string status = ((configuredPath != null) ? "connected" : ((PersistedPath != null) ? "needs_reconnect" : "not_connected"));
		string text = ((configuredPath == null) ? null : Path.GetFileName(configuredPath.TrimEnd('\\', '/')));
		string description = ((text != null) ? ("Local Markdown vault \"" + text + "\". HeyClicky adds the Obsidian skill to spawned agents.") : "Use a local Markdown vault through HeyClicky's Obsidian skill. No Composio account required.");
		return new AgentIntegration("obsidian", "Obsidian", description, status, (configuredPath != null) ? 1 : 0, 1, new global::_003C_003Ez__ReadOnlySingleElementList<AgentIntegrationToolkit>(new AgentIntegrationToolkit("obsidian", "Obsidian", null, configuredPath != null)), Array.Empty<AgentIntegrationAccount>());
	}

	public static bool Configure(nint owner)
	{
		if (store == null)
		{
			return false;
		}
		string startFolder = ConfiguredPath ?? DefaultPickerFolder();
		string text = FolderDialog.Pick(owner, "Select Obsidian Vault", "Configure", startFolder);
		if (text == null)
		{
			return false;
		}
		string path = Path.GetFullPath(text);
		store.Update(delegate(AppSettings s)
		{
			s.ObsidianVaultPath = path;
		});
		Log.Info("settings", "obsidian vault configured (" + Path.GetFileName(path.TrimEnd('\\')) + ")");
		ObsidianVault.Changed?.Invoke();
		return true;
	}

	public static void Unset()
	{
		if (store != null)
		{
			store.Update(delegate(AppSettings s)
			{
				s.ObsidianVaultPath = null;
			});
			Log.Info("settings", "obsidian vault removed");
			ObsidianVault.Changed?.Invoke();
		}
	}

	public static string? DefaultPickerFolder()
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obsidian", "obsidian.json");
			if (File.Exists(path) && JsonNode.Parse(File.ReadAllText(path), null)?["vaults"] is JsonObject source)
			{
				bool value = default(bool);
				long value2;
				(string, bool, long) tuple = (from v in source.Select<KeyValuePair<string, JsonNode>, JsonObject>((KeyValuePair<string, JsonNode> v) => v.Value as JsonObject).OfType<JsonObject>()
					select (Path: v["path"]?.ToString(), Open: v["open"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value, Ts: (v["ts"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<long>(out value2)) ? value2 : 0) into v
					where v.Path != null && Directory.Exists(v.Path)
					orderby v.Open descending, v.Ts descending
					select v).FirstOrDefault();
				if (tuple.Item1 != null)
				{
					return tuple.Item1;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("settings", "obsidian.json unreadable: " + ex.Message);
		}
		return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
	}
}
