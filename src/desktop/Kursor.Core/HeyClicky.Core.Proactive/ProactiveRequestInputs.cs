using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveRequestInputs(IReadOnlyList<string> ConnectedIntegrationNames, IReadOnlyList<ProactiveHistoryEntry> RecentSuggestions, ProactiveCheckReason CheckReason, IReadOnlyList<ProactiveClickyDescriptor> Clickys, IReadOnlyList<(string Name, string Description)> CustomConnectors)
{
	public bool ResearchOnlyForGoodMorning { get; set; }

	public bool ReadsForMorningPaper { get; set; }

	public IReadOnlyList<(string Title, int EveryMinutes)> ExistingRoutines { get; set; } = Array.Empty<(string, int)>();
}
