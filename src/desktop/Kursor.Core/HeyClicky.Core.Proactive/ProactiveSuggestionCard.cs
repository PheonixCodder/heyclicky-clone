using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveSuggestionCard
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public required string Title { get; set; }

	public required string Description { get; set; }

	public required string AgentPrompt { get; set; }

	public ProactiveCheckReason CheckReason { get; set; }

	public int? ServerSuggestionId { get; set; }

	public string? ClickySlug { get; set; }

	public ProactiveNewClicky? NewClicky { get; set; }

	public ProactiveRoutine? Routine { get; set; }

	public string? IntegrationName { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

	public DateTimeOffset? ViewedAt { get; set; }

	public DateTimeOffset? ExpiresAt => CheckReason switch
	{
		ProactiveCheckReason.Onboarding => null, 
		ProactiveCheckReason.Debug => CreatedAt + TimeSpan.FromMinutes(5L), 
		_ => CreatedAt + DailyInterval, 
	};

	public static readonly TimeSpan DailyInterval = TimeSpan.FromHours(24);

	public bool IsExpired(DateTimeOffset now)
	{
		DateTimeOffset? expiresAt = ExpiresAt;
		if (expiresAt.HasValue)
		{
			DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
			return now >= valueOrDefault;
		}
		return false;
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ProactiveSuggestionCard(ProactiveSuggestionCard original)
	{
		Id = original.Id;
		Title = original.Title;
		Description = original.Description;
		AgentPrompt = original.AgentPrompt;
		CheckReason = original.CheckReason;
		ServerSuggestionId = original.ServerSuggestionId;
		ClickySlug = original.ClickySlug;
		NewClicky = original.NewClicky;
		Routine = original.Routine;
		IntegrationName = original.IntegrationName;
		CreatedAt = original.CreatedAt;
		ViewedAt = original.ViewedAt;
	}

	public ProactiveSuggestionCard()
	{
	}
}
