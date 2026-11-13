using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.App.Billing;
using HeyClicky.App.Services;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Input;

namespace HeyClicky.App.Settings;

public sealed class SettingsHost
{
	private static readonly Lazy<SettingsStore> DemoStore = new Lazy<SettingsStore>(() => new SettingsStore(AppPaths.Combine("demo", "core-settings.json")));

	public AppServices Services { get; }

	public IAppActions Actions { get; }

	public AppHost? App { get; }

	public SettingsStore Store { get; }

	public BillingUi? Billing => Services.Billing;

	public WorkerClient? Worker => App?.Real?.Core.Worker;

	public string WorkerBaseUrl => App?.Real?.Core.Endpoints.WorkerBaseUrl.AbsoluteUri ?? "http://127.0.0.1:8787";

	public AgentService? Agents
	{
		get
		{
			RealStack realStack = App?.Real;
			if (realStack == null || !realStack.IsComplete)
			{
				return null;
			}
			return realStack.Agents;
		}
	}

	public ShortcutMonitor? Monitor
	{
		get
		{
			RealStack realStack = App?.Real;
			if (realStack == null || !realStack.IsComplete)
			{
				return null;
			}
			return realStack.Shortcuts;
		}
	}

	public bool IsSignedIn => Services.Auth.IsSignedIn;

	public AppSettings Current => Store.Current;

	public bool ShowsDeveloperSection => (Agents?.AllowsDeveloperModelSelection ?? false) || Environment.GetEnvironmentVariable("KURSOR_DEV") == "1";

	public string BuildInfoLine
	{
		get
		{
			Version version = typeof(SettingsHost).Assembly.GetName().Version ?? new Version(0, 1, 0, 0);
			return $"Kursor {version.Major}.{version.Minor}.{version.Build} ({Math.Max(1, version.Revision)})";
		}
	}

	public static AppConfigLinks Links
	{
		get
		{
			AppRemoteLinks links = AppRemoteConfig.Shared.Links;
			return new AppConfigLinks(links.FeatureRequest, links.CommunityReddit, links.CommunityWhatsApp, links.Changelog);
		}
	}

	public SettingsHost(AppServices services, IAppActions actions)
	{
		Services = services;
		Actions = actions;
		App = actions as AppHost;
		Store = App?.Real?.Core.Settings ?? DemoStore.Value;
	}

	public T Read<T>(Func<AppSettings, T> read)
	{
		return Store.Read(read);
	}

	public void Set(string key, object? value, Action<AppSettings> change)
	{
		Log.Info("settings", $"{key} = {value ?? "null"}");
		Store.Update(change);
		TrackSettingChanged(key, value);
	}

	public static void TrackSettingChanged(string key, object? value)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		string text = key;
		string text2 = "home_space_settings";
		switch (key)
		{
		case "show_in_taskbar":
			text = "show_in_dock";
			jsonObject["value"] = Json(value);
			break;
		case "agent_folder":
			jsonObject["uses_custom_folder"] = value is string text4 && text4 != "default";
			break;
		case "dictation_dictionary":
			if (value is string text5 && text5.StartsWith("add", StringComparison.Ordinal))
			{
				jsonObject["action"] = "add";
				if (int.TryParse(new string(text5.Where(char.IsAsciiDigit).ToArray()), out var result))
				{
					jsonObject["word_count"] = result;
				}
			}
			else
			{
				jsonObject["action"] = "remove";
			}
			break;
		case "shortcuts":
			text2 = "shortcuts";
			jsonObject["action"] = "reset_defaults";
			break;
		default:
			if (key.StartsWith("shortcut_", StringComparison.Ordinal))
			{
				int length = "shortcut_".Length;
				string text3 = key.Substring(length, key.Length - length);
				text = text3 switch
				{
					"PushToTalk" => "voice_shortcut", 
					"TextMode" => "text_shortcut", 
					"Dictation" => "dictation_shortcut", 
					"HandsFreeDictation" => "hands_free_shortcut", 
					_ => text3.ToLowerInvariant() + "_shortcut", 
				};
				text2 = "shortcuts";
				jsonObject["action"] = "saved";
			}
			else if (value != null)
			{
				jsonObject["value"] = Json(value);
			}
			break;
		case "microphone":
			break;
		}
		jsonObject["setting"] = text;
		jsonObject["surface"] = text2;
		ClickyAnalytics.TrackProductEvent("setting_changed", null, null, jsonObject);
		static JsonNode? Json(object? v)
		{
			if (v == null)
			{
				return null;
			}
			if (v is bool flag)
			{
				return flag;
			}
			if (v is int num)
			{
				return num;
			}
			if (v is double num2)
			{
				return num2;
			}
			if (v is float num3)
			{
				return (double)num3;
			}
			return v.ToString();
		}
	}

	public void OpenUrl(string? url)
	{
		if (!string.IsNullOrWhiteSpace(url))
		{
			Log.Info("settings", "open " + ((url.Length > 160) ? (url.Substring(0, 160) + "…") : url));
			if (!(Environment.GetEnvironmentVariable("HEYCLICKY_E2E_NO_BROWSER") == "1"))
			{
				Actions.OpenUrl(url);
			}
		}
	}

	public Task RefreshLinksAsync()
	{
		WorkerClient worker = Worker;
		if (worker == null)
		{
			return Task.CompletedTask;
		}
		return AppRemoteConfig.Shared.RefreshAsync(worker);
	}
}
