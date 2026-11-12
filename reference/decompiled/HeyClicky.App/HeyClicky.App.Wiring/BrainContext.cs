using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Settings;
using HeyClicky.Core;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;

namespace HeyClicky.App.Wiring;

public sealed class BrainContext
{
	private static readonly TimeSpan RefreshAfter = TimeSpan.FromMinutes(10L);

	private readonly CoreServices core;

	private readonly IAgentToolHost agents;

	private volatile PlanSnapshot? plan;

	private volatile BrainUserContext apps = BrainUserContext.Empty;

	private DateTimeOffset fetchedAt = DateTimeOffset.MinValue;

	private int refreshing;

	public volatile ClickyNotes.Report? LastReport;

	public Func<string?>? JobsNote { get; set; }

	public BrainContext(CoreServices core, IAgentToolHost agents)
	{
		this.core = core;
		this.agents = agents;
	}

	public string? RosterNote()
	{
		Func<string> jobsNote = JobsNote;
		if (jobsNote == null)
		{
			return ClickyNotes.RosterNote(agents.Clickys, agents.Tasks, LastReport, DateTimeOffset.Now);
		}
		return jobsNote();
	}

	public string? PlanNote()
	{
		RefreshIfStale();
		return ClickyNotes.PlanNote(plan, core.Billing.DisplayedMessagesUsage);
	}

	public BrainUserContext UserContext()
	{
		RefreshIfStale();
		return WithLocalApps(apps)with
		{
			RosterNote = (RosterNote() ?? "")
		};
	}

	public static BrainUserContext WithLocalApps(BrainUserContext fromCatalog)
	{
		List<string> list = fromCatalog.ConnectedApps.Where((string id) => id != "obsidian" && !id.StartsWith("custom:", StringComparison.Ordinal)).ToList();
		List<string> list2 = fromCatalog.NeedsReconnect.Where((string n) => !string.Equals(n, "Obsidian", StringComparison.OrdinalIgnoreCase)).ToList();
		if (ObsidianVault.ConfiguredPath != null)
		{
			list.Add("obsidian");
		}
		else if (ObsidianVault.PersistedPath != null)
		{
			list2.Add("Obsidian");
		}
		List<CustomConnector> list3 = (from c in CustomConnectorStore.Load()
			orderby c.CreatedAt
			select c).ToList();
		foreach (CustomConnector item in list3)
		{
			if (ConnectorAuth.NeedsReconnect(item.Id))
			{
				list2.Add(item.Name);
			}
			else
			{
				list.Add(item.IntegrationId);
			}
		}
		return fromCatalog with
		{
			ConnectedApps = list,
			NeedsReconnect = list2.Order<string>(StringComparer.OrdinalIgnoreCase).ToList(),
			CustomConnectors = list3.Select((CustomConnector c) => new CustomConnectorDescription(c.Name, c.EffectiveCapabilityDescription)).ToList()
		};
	}

	private void RefreshIfStale()
	{
		if (DateTimeOffset.Now - fetchedAt > RefreshAfter)
		{
			Refresh();
		}
	}

	public void Refresh()
	{
		if (!core.Auth.IsSignedIn || Interlocked.Exchange(ref refreshing, 1) == 1)
		{
			return;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				Task<PlanSnapshot> planAsync = core.Worker.GetPlanAsync();
				Task<JsonObject?> integrationsTask = core.Worker.GetJsonObjectAsync("/agent/integrations");
				try
				{
					plan = (await planAsync.ConfigureAwait(continueOnCapturedContext: false)) ?? plan;
				}
				catch (Exception ex)
				{
					Log.Warn("BrainContext", "/me/plan: " + ex.Message);
				}
				try
				{
					JsonObject jsonObject = await integrationsTask.ConfigureAwait(continueOnCapturedContext: false);
					if (jsonObject != null)
					{
						apps = BrainUserContext.FromIntegrations(jsonObject);
						Log.Info("BrainContext", $"connected apps: [{string.Join(", ", apps.ConnectedApps)}], reconnect: [{string.Join(", ", apps.NeedsReconnect)}]");
					}
				}
				catch (Exception ex2)
				{
					Log.Warn("BrainContext", "/agent/integrations: " + ex2.Message);
				}
				fetchedAt = DateTimeOffset.Now;
			}
			finally
			{
				Interlocked.Exchange(ref refreshing, 0);
			}
		});
	}
}
