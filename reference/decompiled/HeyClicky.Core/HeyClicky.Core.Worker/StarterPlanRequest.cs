using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record StarterPlanRequest(IReadOnlyList<InterviewLine> Interview, IReadOnlyList<string> ConnectedIntegrations, IReadOnlyList<string> AvailableIntegrations, IReadOnlyList<StarterPlanClickyInput>? Clickys = null, string? FirstAsksForClicky = null, bool? InterviewSaved = null)
{
	public bool Jobs { get; init; } = true;
}
