using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed class ProactiveStoreData
{
	public Dictionary<string, ProactiveAccountState> Accounts { get; set; } = new Dictionary<string, ProactiveAccountState>();

	public List<ProactiveHistoryEntry> History { get; set; } = new List<ProactiveHistoryEntry>();

	public string? MorningGreetingLastDay { get; set; }

	public DateTimeOffset? MorningGreetingLastPresentedAt { get; set; }

	public DateTimeOffset? MorningSettledAt { get; set; }

	public MorningDismissalHistory MorningDismissals { get; set; } = MorningDismissalHistory.Empty;

	public DateTimeOffset? OnboardingFirstCompletedAt { get; set; }

	public Dictionary<string, DateTimeOffset> IntegrationSnoozedUntil { get; set; } = new Dictionary<string, DateTimeOffset>();

	public List<string> IntegrationNeverAsk { get; set; } = new List<string>();

	public List<DateTimeOffset> IntegrationPromptTimestamps { get; set; } = new List<DateTimeOffset>();

	public bool SpotifyVoiceTipShown { get; set; }
}
