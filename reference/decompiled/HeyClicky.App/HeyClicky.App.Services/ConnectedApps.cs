using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Backend;
using HeyClicky.App.Settings;

namespace HeyClicky.App.Services;

public static class ConnectedApps
{
	private static readonly TimeSpan RefreshAfter = TimeSpan.FromMinutes(10L);

	private static AgentService? agents;

	private static Func<string>? workerBaseUrl;

	private static AgentIntegrationCatalog? catalog;

	private static DateTimeOffset fetchedAt;

	private static int refreshing;

	private static readonly string[] Recommended = new string[11]
	{
		"notion", "linear", "github", "googledocs", "googlecalendar", "linkedin", "slack", "gmail", "googlesheets", "googledrive",
		"obsidian"
	};

	public static AgentIntegrationCatalog? Catalog => catalog;

	public static event Action? Changed;

	public static void Initialize(AgentService service, Func<string> worker)
	{
		agents = service;
		workerBaseUrl = worker;
		ConnectorAuth.Changed += delegate
		{
			ConnectedApps.Changed?.Invoke();
		};
		ObsidianVault.Changed += delegate
		{
			ConnectedApps.Changed?.Invoke();
		};
	}

	public static void SetCatalog(AgentIntegrationCatalog fresh)
	{
		catalog = fresh;
		fetchedAt = DateTimeOffset.Now;
		ConnectedApps.Changed?.Invoke();
	}

	public static void RefreshIfStale()
	{
		if (DateTimeOffset.Now - fetchedAt > RefreshAfter)
		{
			RefreshAsync();
		}
	}

	public static async Task RefreshAsync()
	{
		if (agents == null || Interlocked.Exchange(ref refreshing, 1) == 1)
		{
			return;
		}
		try
		{
			AgentIntegrationCatalog agentIntegrationCatalog = await agents.ListIntegrationsAsync().ConfigureAwait(continueOnCapturedContext: false);
			SetCatalog(agentIntegrationCatalog);
			Log.Info("apps", $"catalog: {agentIntegrationCatalog.Integrations.Count} apps, {agentIntegrationCatalog.Integrations.Count((AgentIntegration i) => i.IsActiveConnection)} connected");
		}
		catch (Exception ex)
		{
			Log.Warn("apps", "/agent/integrations: " + ex.Message);
		}
		finally
		{
			Interlocked.Exchange(ref refreshing, 0);
		}
	}

	public static IReadOnlyList<AppEntry> All()
	{
		List<AppEntry> list = new List<AppEntry>();
		foreach (AgentIntegration item in catalog?.Integrations ?? Array.Empty<AgentIntegration>())
		{
			if (!item.IsLocalIntegration)
			{
				list.Add(new AppEntry(item.Id, item.DisplayName, item.PrimaryToolkitSlug, item.LogoUrl, item.Status, item.Description));
			}
		}
		AgentIntegration agentIntegration = ObsidianVault.Row();
		list.Add(new AppEntry(agentIntegration.Id, agentIntegration.DisplayName, "obsidian", null, agentIntegration.Status, agentIntegration.Description, IsLocal: true));
		string text = workerBaseUrl?.Invoke() ?? "";
		foreach (CustomConnector item2 in from c in CustomConnectorStore.Load()
			orderby c.CreatedAt
			select c)
		{
			list.Add(new AppEntry(item2.IntegrationId, item2.Name, item2.ServerKey, item2.LogoUrl(text), ConnectorAuth.NeedsReconnect(item2.Id) ? "needs_reconnect" : "connected", (item2.EffectiveCapabilityDescription.Length > 0) ? item2.EffectiveCapabilityDescription : ("Custom MCP connector · " + item2.LocationSummary), IsLocal: true));
		}
		return list.OrderBy((AppEntry r) => RecommendedRank(r) ?? int.MaxValue).ThenBy<AppEntry, string>((AppEntry r) => r.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	public static IReadOnlyList<AppEntry> Connected()
	{
		return (from a in All()
			where a.Status == "connected"
			select a).ToList();
	}

	private static int? RecommendedRank(AppEntry a)
	{
		int num = Array.FindIndex(Recommended, (string r) => string.Equals(r, a.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(r, a.ToolkitSlug, StringComparison.OrdinalIgnoreCase));
		if (num >= 0)
		{
			return num;
		}
		return null;
	}

	public static AppEntry? Find(string providerKey)
	{
		string key = providerKey.Trim().ToLowerInvariant();
		foreach (AppEntry item in All())
		{
			if (string.Equals(item.Id, key, StringComparison.OrdinalIgnoreCase) || string.Equals(item.ToolkitSlug, key, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		AgentIntegration agentIntegration = catalog?.Integrations.FirstOrDefault((AgentIntegration i) => i.Toolkits.Any((AgentIntegrationToolkit t) => string.Equals(t.Slug, key, StringComparison.OrdinalIgnoreCase)));
		if ((object)agentIntegration != null)
		{
			return new AppEntry(agentIntegration.Id, agentIntegration.DisplayName, agentIntegration.PrimaryToolkitSlug, agentIntegration.LogoUrl, agentIntegration.Status, agentIntegration.Description);
		}
		return null;
	}

	public static IReadOnlyList<AppEntry> SampleApps()
	{
		List<AppEntry> list = (from i in catalog?.Integrations ?? Array.Empty<AgentIntegration>()
			where !i.IsLocalIntegration
			select new AppEntry(i.Id, i.DisplayName, i.PrimaryToolkitSlug, i.LogoUrl, "connected", i.Description)).ToList();
		List<AppEntry> sample = new List<AppEntry>();
		string[] array = new string[4] { "gmail", "googlecalendar", "notion", "slack" };
		foreach (string key in array)
		{
			AppEntry appEntry = list.FirstOrDefault((AppEntry a) => !sample.Contains(a) && (a.Id + " " + a.ToolkitSlug + " " + a.DisplayName).ToLowerInvariant().Replace(" ", "").Contains(key, StringComparison.Ordinal));
			if ((object)appEntry != null)
			{
				sample.Add(appEntry);
			}
		}
		foreach (AppEntry item in list)
		{
			if (sample.Count >= 4)
			{
				break;
			}
			if (!sample.Contains(item))
			{
				sample.Add(item);
			}
		}
		return sample;
	}
}
