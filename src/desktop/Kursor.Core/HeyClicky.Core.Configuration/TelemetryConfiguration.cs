using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HeyClicky.Core.Configuration;

public sealed record TelemetryConfiguration(string? PostHogApiKey, string PostHogHost, string? SentryDsn, string UpdateFeedUrl)
{
	public static TelemetryConfiguration Current { get; private set; } = Load();

	public bool HasPostHogKey => !string.IsNullOrWhiteSpace(PostHogApiKey);

	public bool HasSentryDsn => !string.IsNullOrWhiteSpace(SentryDsn);

	public bool IsPlaceholderFeed => UpdateFeedUrl.StartsWith("https://heyclicky-releases.invalid/windows", StringComparison.OrdinalIgnoreCase);

	public const string DefaultPostHogHost = "https://us.i.posthog.com";

	public const string PlaceholderUpdateFeedUrl = "https://heyclicky-releases.invalid/windows";

	public static TelemetryConfiguration Reload(string? overrideFilePath = null, Assembly? entry = null)
	{
		return Current = Load(overrideFilePath, entry);
	}

	public static TelemetryConfiguration Load(string? overrideFilePath = null, Assembly? entry = null)
	{
		if ((object)entry == null)
		{
			entry = Assembly.GetEntryAssembly();
		}
		string postHogApiKey = Meta("HeyClicky.PostHogApiKey");
		string text = Meta("HeyClicky.PostHogHost") ?? "https://us.i.posthog.com";
		string sentryDsn = Meta("HeyClicky.SentryDsn");
		string updateFeedUrl = Meta("HeyClicky.UpdateFeedUrl") ?? "https://heyclicky-releases.invalid/windows";
		string path = overrideFilePath ?? AppBundleConfiguration.OverrideFilePath;
		try
		{
			if (File.Exists(path))
			{
				ReadOnlyMemory<byte> utf8Json = File.ReadAllBytes(path);
				if (utf8Json.Span.StartsWith("\ufeff"u8))
				{
					utf8Json = utf8Json.Slice(3, utf8Json.Length - 3);
				}
				using JsonDocument jsonDocument = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
				{
					CommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true
				});
				JsonElement rootElement = jsonDocument.RootElement;
				if (Read(rootElement, "postHogApiKey", out string value))
				{
					postHogApiKey = value;
				}
				if (Read(rootElement, "postHogHost", out string value2) && value2 != null)
				{
					text = value2;
				}
				if (Read(rootElement, "sentryDsn", out string value3))
				{
					sentryDsn = value3;
				}
				if (Read(rootElement, "updateFeedUrl", out string value4) && value4 != null)
				{
					updateFeedUrl = value4;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Config", "endpoints.json telemetry keys ignored: " + ex.Message);
		}
		return new TelemetryConfiguration(postHogApiKey, text.TrimEnd('/'), sentryDsn, updateFeedUrl);
		string? Meta(string key)
		{
			return Clean(entry?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault((AssemblyMetadataAttribute a) => a.Key == key)?.Value);
		}
	}

	private static bool Read(JsonElement root, string name, out string? value)
	{
		value = null;
		if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var value2))
		{
			return false;
		}
		if (value2.ValueKind == JsonValueKind.Null)
		{
			return true;
		}
		if (value2.ValueKind != JsonValueKind.String)
		{
			return false;
		}
		value = Clean(value2.GetString());
		return true;
	}

	private static string? Clean(string? text)
	{
		text = text?.Trim();
		if (!string.IsNullOrEmpty(text) && (!text.StartsWith("$(") || !text.EndsWith(')')))
		{
			return text;
		}
		return null;
	}

	[CompilerGenerated]
	private TelemetryConfiguration(TelemetryConfiguration original)
	{
		PostHogApiKey = original.PostHogApiKey;
		PostHogHost = original.PostHogHost;
		SentryDsn = original.SentryDsn;
		UpdateFeedUrl = original.UpdateFeedUrl;
	}
}
