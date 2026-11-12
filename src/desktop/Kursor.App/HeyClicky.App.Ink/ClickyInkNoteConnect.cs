using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Backend;
using HeyClicky.App.Services;
using HeyClicky.Core.Ink;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkNoteConnect
{
	public const string IntegrationSource = "ink_note";

	private readonly AgentService agents;

	private readonly DispatcherQueue dispatcher;

	private readonly Action<string> openUrl;

	private readonly HashSet<string> pending = new HashSet<string>();

	private readonly HashSet<string> awaitingCallback = new HashSet<string>();

	private readonly Dictionary<string, string> replacedAccounts = new Dictionary<string, string>();

	private CancellationTokenSource? refresh;

	public Action? BringAppToFront { get; set; }

	public ClickyInkNoteConnect(AgentService agents, DispatcherQueue dispatcher, Action<string> openUrl)
	{
		this.agents = agents;
		this.dispatcher = dispatcher;
		this.openUrl = openUrl;
	}

	private static string Lowered(string value)
	{
		return value.Trim().ToLowerInvariant();
	}

	public static HashSet<string> IdentifierCandidates(AgentIntegration integration)
	{
		HashSet<string> hashSet = new HashSet<string>
		{
			Lowered(integration.Id),
			Lowered(integration.PrimaryToolkitSlug)
		};
		foreach (AgentIntegrationToolkit toolkit in integration.Toolkits)
		{
			hashSet.Add(Lowered(toolkit.Slug));
		}
		hashSet.Remove("");
		return hashSet;
	}

	public void Sync(IReadOnlyList<AgentIntegration>? catalog)
	{
		ClickyInkConnections shared;
		ClickyInkConnections clickyInkConnections = (shared = ClickyInkConnections.Shared);
		if (shared.Connect == null)
		{
			Action<string> action2 = (shared.Connect = ConnectFromNote);
		}
		IReadOnlyList<AgentIntegration> readOnlyList = catalog ?? Array.Empty<AgentIntegration>();
		clickyInkConnections.SetApps(from i in readOnlyList
			where !i.IsLocalIntegration
			select new ClickyInkConnections.App(i.PrimaryToolkitSlug, i.DisplayName, Uri.TryCreate(i.LogoUrl, UriKind.Absolute, out Uri result) ? result : null, i.IsActiveConnection, i.Status == "needs_reconnect"));
		TrackPendingConnections(readOnlyList);
	}

	private AgentIntegration? IntegrationFor(string normalizedSlug)
	{
		return (ConnectedApps.Catalog?.Integrations ?? Array.Empty<AgentIntegration>()).FirstOrDefault((AgentIntegration integration) => IdentifierCandidates(integration).Any((string candidate) => ClickyInkConnections.Normalized(candidate) == normalizedSlug));
	}

	private void ConnectFromNote(string normalizedSlug)
	{
		AgentIntegration agentIntegration = IntegrationFor(normalizedSlug);
		if ((object)agentIntegration != null)
		{
			StartSignIn(agentIntegration, normalizedSlug);
			return;
		}
		Task.Run(async delegate
		{
			await ConnectedApps.RefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
			dispatcher.TryEnqueue(delegate
			{
				AgentIntegration agentIntegration2 = IntegrationFor(normalizedSlug);
				if ((object)agentIntegration2 == null)
				{
					ClickyInkConnections.Shared.SetPhase(new ClickyInkConnections.Phase.Failed("HeyClicky can't connect this app yet."), normalizedSlug);
				}
				else
				{
					StartSignIn(agentIntegration2, normalizedSlug);
				}
			});
		});
	}

	private void StartSignIn(AgentIntegration integration, string normalizedSlug)
	{
		string text = ((!(integration.Status == "needs_reconnect")) ? null : integration.Accounts.FirstOrDefault((AgentIntegrationAccount a) => !a.IsActive)?.ConnectedAccountId);
		string id = Lowered(integration.PrimaryToolkitSlug);
		if (text != null)
		{
			replacedAccounts[id] = text;
		}
		else
		{
			replacedAccounts.Remove(id);
		}
		pending.Add(id);
		awaitingCallback.Add(id);
		ClickyAnalytics.TrackIntegrationInitiated(integration.Id, integration.DisplayName, integration.Toolkits.Select((AgentIntegrationToolkit t) => t.Slug).ToList(), "ink_note", integration.Status, integration.ConnectedCount, integration.TotalCount);
		Task.Run(async delegate
		{
			string url = null;
			try
			{
				url = await agents.ConnectIntegrationAsync(integration.PrimaryToolkitSlug).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "connect " + integration.Id + " from a note failed: " + ex.Message);
			}
			dispatcher.TryEnqueue(delegate
			{
				if (url == null)
				{
					pending.Remove(id);
					awaitingCallback.Remove(id);
					replacedAccounts.Remove(id);
				}
				else
				{
					Log.Info("ink", "note connect: " + integration.Id + " → browser");
					openUrl(url);
				}
				ClickyInkConnections.Shared.SetPhase((url != null) ? ((ClickyInkConnections.Phase)new ClickyInkConnections.Phase.WaitingForSignIn()) : ((ClickyInkConnections.Phase)new ClickyInkConnections.Phase.Failed("Couldn't open the sign-in page. Try again.")), normalizedSlug);
			});
		});
	}

	public bool OnAuthCallback(string? integrationID, bool didFail)
	{
		if (integrationID == null)
		{
			return false;
		}
		string id = Lowered(integrationID);
		if (!pending.Contains(id))
		{
			return false;
		}
		awaitingCallback.Remove(id);
		string slug = ClickyInkConnections.Normalized(id);
		if (ClickyInkConnections.Shared.IsConnectingFromNote(slug))
		{
			ClickyInkConnections.Shared.SetPhase(didFail ? ((ClickyInkConnections.Phase)new ClickyInkConnections.Phase.Failed("The sign-in wasn't finished. Try again.")) : ((ClickyInkConnections.Phase)new ClickyInkConnections.Phase.Checking()), slug);
			BringAppToFront?.Invoke();
		}
		if (didFail)
		{
			pending.Remove(id);
			replacedAccounts.Remove(id);
			return true;
		}
		refresh?.Cancel();
		CancellationTokenSource cancel = (refresh = new CancellationTokenSource());
		Task.Run(async delegate
		{
			for (int attempt = 0; attempt < 4; attempt++)
			{
				if (cancel.IsCancellationRequested)
				{
					return;
				}
				if (attempt > 0)
				{
					await Task.Delay(TimeSpan.FromSeconds(attempt)).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (cancel.IsCancellationRequested)
				{
					return;
				}
				await ConnectedApps.RefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (!(await OnUi(delegate
				{
					TrackPendingConnections(ConnectedApps.Catalog?.Integrations ?? Array.Empty<AgentIntegration>());
					return pending.Contains(id);
				}).ConfigureAwait(continueOnCapturedContext: false)))
				{
					return;
				}
			}
			if (!cancel.IsCancellationRequested)
			{
				dispatcher.TryEnqueue(delegate
				{
					if (pending.Contains(id))
					{
						StillSyncing(id);
					}
				});
			}
		});
		return true;
	}

	private Task<T> OnUi<T>(Func<T> work)
	{
		TaskCompletionSource<T> done = new TaskCompletionSource<T>();
		if (!dispatcher.TryEnqueue(delegate
		{
			try
			{
				done.SetResult(work());
			}
			catch (Exception exception)
			{
				done.SetException(exception);
			}
		}))
		{
			done.SetCanceled();
		}
		return done.Task;
	}

	private static void StillSyncing(string integrationID)
	{
		string slug = ClickyInkConnections.Normalized(integrationID);
		if (ClickyInkConnections.Shared.PhaseFor(slug) is ClickyInkConnections.Phase.Checking)
		{
			ClickyInkConnections.Shared.SetPhase(new ClickyInkConnections.Phase.Failed("It's still syncing. Try again in a moment."), slug);
		}
	}

	private void TrackPendingConnections(IReadOnlyList<AgentIntegration> integrations)
	{
		if (pending.Count == 0)
		{
			return;
		}
		foreach (AgentIntegration integration in integrations.Where((AgentIntegration i) => i.IsActiveConnection))
		{
			HashSet<string> hashSet = IdentifierCandidates(integration);
			if (hashSet.Overlaps(awaitingCallback) || !hashSet.Overlaps(pending))
			{
				continue;
			}
			ClickyAnalytics.TrackIntegrationConnected(integration.Id, integration.DisplayName, integration.Toolkits.Select((AgentIntegrationToolkit t) => t.Slug).ToList(), "ink_note", integration.Status, integration.ConnectedCount, integration.TotalCount);
			string replacedID = hashSet.Select((string c) => replacedAccounts.GetValueOrDefault(c)).FirstOrDefault((string r) => r != null);
			if (replacedID != null)
			{
				AgentIntegrationAccount agentIntegrationAccount = integration.Accounts.FirstOrDefault((AgentIntegrationAccount a) => a.ConnectedAccountId == replacedID);
				if ((object)agentIntegrationAccount != null && !agentIntegrationAccount.IsActive)
				{
					Task.Run(async delegate
					{
						try
						{
							await agents.DisconnectIntegrationAsync(integration.PrimaryToolkitSlug, replacedID).ConfigureAwait(continueOnCapturedContext: false);
						}
						catch (Exception ex)
						{
							Log.Warn("ink", "remove the expired " + integration.Id + " account: " + ex.Message);
						}
					});
				}
			}
			foreach (string item in hashSet)
			{
				string slug = ClickyInkConnections.Normalized(item);
				if (ClickyInkConnections.Shared.IsConnectingFromNote(slug))
				{
					ClickyInkConnections.Shared.SetPhase(new ClickyInkConnections.Phase.Connected(), slug);
				}
				pending.Remove(item);
				replacedAccounts.Remove(item);
			}
		}
	}
}
