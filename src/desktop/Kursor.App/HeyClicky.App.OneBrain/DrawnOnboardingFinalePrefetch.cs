using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.App.Shell;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;

namespace HeyClicky.App.OneBrain;

public sealed class DrawnOnboardingFinalePrefetch
{
	public sealed record Prefetched(CompanionSession.PrefetchedTurn Turn, int PlannedCardCount, DateTimeOffset StartedAt, int CodeCharactersReady);

	private const string Area = "drawn tour";

	public const string FinaleFact = "[App] The welcome tour's last moment: the user pressed Show me. Draw their welcome now, as the note says.";

	private CompanionSession.PrefetchedTurn? turn;

	private CompanionSession? companion;

	private int plannedCardCount;

	private bool isStarting;

	private int generation;

	public static DrawnOnboardingFinalePrefetch Shared { get; } = new DrawnOnboardingFinalePrefetch();

	private bool IsActive
	{
		get
		{
			if (!isStarting)
			{
				CompanionSession.PrefetchedTurn prefetchedTurn = turn;
				if (prefetchedTurn != null)
				{
					return !prefetchedTurn.IsDropped;
				}
				return false;
			}
			return true;
		}
	}

	public bool IsPrefetching
	{
		get
		{
			CompanionSession.PrefetchedTurn prefetchedTurn = turn;
			if (prefetchedTurn != null && !prefetchedTurn.IsDropped)
			{
				return !prefetchedTurn.IsAdopted;
			}
			return false;
		}
	}

	public int CodeCharactersReady => turn?.CodeCharactersReady ?? 0;

	private DrawnOnboardingFinalePrefetch()
	{
	}

	public void StartIfReady(AppHost host)
	{
		if (!IsActive && host.IsDrawnOnboardingFinalePrefetchReady)
		{
			RealStack real = host.Real;
			if (real != null)
			{
				isStarting = true;
				StartAsync(host, real, generation);
			}
		}
	}

	private async Task StartAsync(AppHost host, RealStack real, int generationAtStart)
	{
		CompanionSession.PrefetchedTurn prepared = null;
		PanelWindow panel = host.TourPanelWindow;
		panel?.SetTemporarilyHiddenFromOwnCapture(hidden: true);
		try
		{
			prepared = await real.Companion.PreparePrefetchedFactTurnAsync("[App] The welcome tour's last moment: the user pressed Show me. Draw their welcome now, as the note says.");
		}
		catch (Exception ex)
		{
			Log.Warn("drawn tour", "finale prefetch not started: " + ex.Message);
		}
		finally
		{
			panel?.SetTemporarilyHiddenFromOwnCapture(hidden: false);
		}
		if (generation != generationAtStart || !host.IsDrawnOnboardingFinalePrefetchReady || prepared == null)
		{
			if (generation == generationAtStart)
			{
				isStarting = false;
			}
			return;
		}
		IReadOnlyList<InterviewLine> interview = host.Interview?.Transcript ?? Array.Empty<InterviewLine>();
		List<ProactiveSuggestionCard> list = (host.Proactive?.VisibleCards ?? Array.Empty<ProactiveSuggestionCard>()).Where((ProactiveSuggestionCard c) => c.CheckReason == ProactiveCheckReason.Onboarding).Take(3).ToList();
		JsonObject flowBrief = DrawnOnboardingTour.TourBrief(host.AccountFirstNameForTour ?? "there", interview, host.SpokenTalkKeys(), list);
		isStarting = false;
		if (real.Companion.StartPrefetchedTurn(prepared, flowBrief))
		{
			turn = prepared;
			companion = real.Companion;
			plannedCardCount = list.Count;
			Log.Info("drawn tour", $"finale: prefetching on the finale page ({list.Count} planned card(s))");
		}
	}

	public Prefetched? Take(string? currentAccountId)
	{
		CompanionSession.PrefetchedTurn prefetchedTurn = turn;
		if (prefetchedTurn != null)
		{
			CompanionSession companionSession = companion;
			if (companionSession != null && !prefetchedTurn.IsAdopted)
			{
				if (prefetchedTurn.IsDropped || prefetchedTurn.AccountId != currentAccountId || prefetchedTurn.DidFail)
				{
					if (prefetchedTurn.DidFail && !prefetchedTurn.IsDropped)
					{
						Log.Info("drawn tour", "finale prefetch failed (" + prefetchedTurn.Failure + "): Show me runs the turn the old way");
					}
					Reset();
					return null;
				}
				if (!companionSession.AdoptPrefetchedTurn(prefetchedTurn))
				{
					Reset();
					return null;
				}
				return new Prefetched(prefetchedTurn, plannedCardCount, prefetchedTurn.StartedAt, prefetchedTurn.CodeCharactersReady);
			}
		}
		return null;
	}

	public void DiscardUnlessAdopted(string reason)
	{
		if (IsActive)
		{
			CompanionSession.PrefetchedTurn? prefetchedTurn = turn;
			if (prefetchedTurn == null || !prefetchedTurn.IsAdopted)
			{
				Log.Info("drawn tour", "finale prefetch dropped: " + reason);
				Reset(reason);
			}
		}
	}

	public void Reset(string reason = "the finale is over")
	{
		generation++;
		CompanionSession.PrefetchedTurn prefetchedTurn = turn;
		if (prefetchedTurn != null && !prefetchedTurn.IsAdopted)
		{
			companion?.DropPrefetchedTurn(reason);
		}
		turn = null;
		companion = null;
		plannedCardCount = 0;
		isStarting = false;
	}
}
