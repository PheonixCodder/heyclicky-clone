namespace HeyClicky.App.Portraits;

public sealed record StarterTaskCard(string Title, string Description, string AgentPrompt, string? ClickySlug, string? RoutineTitle, int? RoutineEveryMinutes, int? SuggestionId, string? Deliverable)
{
	public bool IsRoutine
	{
		get
		{
			int? routineEveryMinutes = RoutineEveryMinutes;
			if (routineEveryMinutes.HasValue)
			{
				return routineEveryMinutes.GetValueOrDefault() > 0;
			}
			return false;
		}
	}
}
