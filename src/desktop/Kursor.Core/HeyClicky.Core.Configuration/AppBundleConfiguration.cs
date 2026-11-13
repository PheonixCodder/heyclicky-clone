using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace HeyClicky.Core.Configuration;

public static class AppBundleConfiguration
{
	public const string ProductionWorkerBaseUrl = "http://127.0.0.1:8787";

	public const string ProductionSupabaseUrl = "http://127.0.0.1:8787";

	public const string ProductionSupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

	public const string LocalWorkerBaseUrl = "http://127.0.0.1:8787";

	public const string LocalSupabaseUrl = "http://127.0.0.1:8787";

	public const string LocalSupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

	public const string WorkerOverrideVariable = "HEYCLICKY_WORKER_BASE_URL";

	public static string AppDataDirectory { get; } = AppPaths.Root;

	public static string OverrideFilePath => Path.Combine(AppDataDirectory, "endpoints.json");

	public static ClickyEndpoints Defaults => new ClickyEndpoints(new Uri("http://127.0.0.1:8787"), new Uri("http://127.0.0.1:8787"), LocalSupabaseAnonKey);

	public static string UseProductionBackendFlagPath => Path.Combine(AppDataDirectory, "use-production-backend.flag");

	public static bool UseProductionBackendRequested => false;

	public static bool IsProductionBackendOverrideActive { get; private set; }

	public static ClickyEndpoints Load(string? overrideFilePath = null)
	{
		IsProductionBackendOverrideActive = false;
		return LoadConfigured(overrideFilePath);
	}

	private static ClickyEndpoints LoadConfigured(string? overrideFilePath)
	{
		ClickyEndpoints clickyEndpoints = WithBuildMetadata(Defaults);
		string path = overrideFilePath ?? OverrideFilePath;
		string? workerEnv = Environment.GetEnvironmentVariable("KURSOR_WORKER_BASE_URL") ?? Environment.GetEnvironmentVariable("HEYCLICKY_WORKER_BASE_URL");
		try
		{
			if (!File.Exists(path))
			{
				return ApplyWorkerOverride(clickyEndpoints, workerEnv);
			}
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
			string text = ReadConfigured(rootElement, "workerBaseUrl");
			string text2 = ReadConfigured(rootElement, "supabaseUrl");
			string text3 = ReadConfigured(rootElement, "supabaseAnonKey");
			clickyEndpoints = new ClickyEndpoints((text == null) ? clickyEndpoints.WorkerBaseUrl : new Uri(text.TrimEnd('/')), (text2 == null) ? clickyEndpoints.SupabaseUrl : new Uri(text2.TrimEnd('/')), text3 ?? clickyEndpoints.SupabaseAnonKey);
			Log.Info("Config", $"endpoints.json override: worker={clickyEndpoints.WorkerBaseUrl} supabase={clickyEndpoints.SupabaseUrl}");
		}
		catch (Exception ex)
		{
			Log.Warn("Config", "endpoints.json ignored: " + ex.Message);
		}
		return ApplyWorkerOverride(clickyEndpoints, workerEnv);
	}

	public static ClickyEndpoints ApplyWorkerOverride(ClickyEndpoints endpoints, string? workerBaseUrl)
	{
		ClickyEndpoints updated = endpoints;
		string? effectiveWorker = string.IsNullOrWhiteSpace(workerBaseUrl)
			? (Environment.GetEnvironmentVariable("KURSOR_WORKER_BASE_URL") ?? Environment.GetEnvironmentVariable("HEYCLICKY_WORKER_BASE_URL"))
			: workerBaseUrl;
		if (!string.IsNullOrWhiteSpace(effectiveWorker))
		{
			if (Uri.TryCreate(effectiveWorker.Trim().TrimEnd('/'), UriKind.Absolute, out Uri? workerUri) && IsLocalUrl(workerUri))
			{
				Log.Info("Config", $"{"HEYCLICKY_WORKER_BASE_URL"}: worker={workerUri}");
				updated = updated with
				{
					WorkerBaseUrl = workerUri
				};
			}
			else
			{
				Log.Warn("Config", "HEYCLICKY_WORKER_BASE_URL ignored (loopback URLs only)");
			}
		}
		string? supabaseUrl = Environment.GetEnvironmentVariable("KURSOR_SUPABASE_URL") ?? Environment.GetEnvironmentVariable("HEYCLICKY_SUPABASE_URL");
		if (!string.IsNullOrWhiteSpace(supabaseUrl) && Uri.TryCreate(supabaseUrl.Trim().TrimEnd('/'), UriKind.Absolute, out Uri? supabaseUri) && IsLocalUrl(supabaseUri))
		{
			updated = updated with
			{
				SupabaseUrl = supabaseUri
			};
		}
		string? supabaseAnonKey = Environment.GetEnvironmentVariable("KURSOR_SUPABASE_ANON_KEY") ?? Environment.GetEnvironmentVariable("HEYCLICKY_SUPABASE_ANON_KEY");
		if (!string.IsNullOrWhiteSpace(supabaseAnonKey))
		{
			updated = updated with
			{
				SupabaseAnonKey = supabaseAnonKey.Trim()
			};
		}
		return updated;
	}

	private static ClickyEndpoints WithBuildMetadata(ClickyEndpoints endpoints)
	{
		try
		{
			Assembly entry = Assembly.GetEntryAssembly();
			if ((object)entry == null)
			{
				return endpoints;
			}
			string text = Meta("HeyClicky.WorkerBaseUrl");
			string text2 = Meta("HeyClicky.SupabaseUrl");
			string text3 = Meta("HeyClicky.SupabaseAnonKey");
			if (text == null && text2 == null && text3 == null)
			{
				return endpoints;
			}
			string text4 = ((text2 != null && IsLocalUrl(new Uri(text2))) ? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0" : endpoints.SupabaseAnonKey);
			return new ClickyEndpoints((text == null) ? endpoints.WorkerBaseUrl : new Uri(text.TrimEnd('/')), (text2 == null) ? endpoints.SupabaseUrl : new Uri(text2.TrimEnd('/')), text3 ?? text4);
			string? Meta(string key)
			{
				object[] customAttributes = entry.GetCustomAttributes(typeof(AssemblyMetadataAttribute), inherit: false);
				for (int i = 0; i < customAttributes.Length; i++)
				{
					if (customAttributes[i] is AssemblyMetadataAttribute assemblyMetadataAttribute && assemblyMetadataAttribute.Key == key && !string.IsNullOrWhiteSpace(assemblyMetadataAttribute.Value))
					{
						return assemblyMetadataAttribute.Value.Trim();
					}
				}
				return null;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Config", "build endpoints ignored: " + ex.Message);
			return endpoints;
		}
	}

	private static string? ReadConfigured(JsonElement root, string name)
	{
		if (root.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		foreach (JsonProperty prop in root.EnumerateObject())
		{
			if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
			{
				string? text = prop.Value.GetString()?.Trim();
				if (!string.IsNullOrEmpty(text) && (!text.StartsWith("$(") || !text.EndsWith(')')))
				{
					return text;
				}
			}
		}
		return null;
	}

	public static bool IsLocalUrl(Uri url)
	{
		switch (url.Host.ToLowerInvariant().Trim('[', ']'))
		{
		case "localhost":
		case "127.0.0.1":
		case "::1":
			return true;
		default:
			return false;
		}
	}

	public static bool IsLocalDevelopmentConfiguration(ClickyEndpoints endpoints)
	{
		if (!IsLocalUrl(endpoints.WorkerBaseUrl))
		{
			return IsLocalUrl(endpoints.SupabaseUrl);
		}
		return true;
	}
}
