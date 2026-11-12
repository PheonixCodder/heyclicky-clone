using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveSuggestionCard
{
	public Guid Id { get; init; } = Guid.NewGuid();

	public required string Title { get; init; }

	public required string Description { get; init; }

	public required string AgentPrompt { get; init; }

	public ProactiveCheckReason CheckReason { get; init; }

	public int? ServerSuggestionId { get; init; }

	public string? ClickySlug { get; init; }

	public ProactiveNewClicky? NewClicky { get; init; }

	public ProactiveRoutine? Routine { get; init; }

	public string? IntegrationName { get; init; }

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

	public DateTimeOffset? ViewedAt { get; init; }

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
