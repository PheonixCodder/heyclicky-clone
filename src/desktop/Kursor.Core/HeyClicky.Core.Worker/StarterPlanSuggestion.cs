namespace HeyClicky.Core.Worker;

public sealed record StarterPlanSuggestion(string Title, string Description, string AgentPrompt, string? Deliverable = null, string? ClickySlug = null, string? ClickyKey = null, StarterPlanRoutine? Routine = null, int? SuggestionId = null);
