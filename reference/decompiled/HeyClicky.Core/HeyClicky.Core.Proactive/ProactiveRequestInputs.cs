using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveRequestInputs(IReadOnlyList<string> ConnectedIntegrationNames, IReadOnlyList<ProactiveHistoryEntry> RecentSuggestions, ProactiveCheckReason CheckReason, IReadOnlyList<ProactiveClickyDescriptor> Clickys, IReadOnlyList<(string Name, string Description)> CustomConnectors)
{
	public bool ResearchOnlyForGoodMorning { get; init; }

	public bool ReadsForMorningPaper { get; init; }

	public IReadOnlyList<(string Title, int EveryMinutes)> ExistingRoutines { get; init; } = Array.Empty<(string, int)>();
}
