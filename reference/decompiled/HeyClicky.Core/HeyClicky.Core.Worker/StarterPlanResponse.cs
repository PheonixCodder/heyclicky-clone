using System.Collections.Generic;

namespace HeyClicky.Core.Worker;

public sealed record StarterPlanResponse(IReadOnlyList<StarterPlanClicky>? Clickys, IReadOnlyList<StarterPlanSuggestion>? SuggestedAgents, string? AgentFamiliarity, string? GoalLine);
