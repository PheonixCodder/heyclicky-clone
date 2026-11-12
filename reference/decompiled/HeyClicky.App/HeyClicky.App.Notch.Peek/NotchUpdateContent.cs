using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Notch.Peek;

public sealed record NotchUpdateContent
{
	public required string RailKey { get; init; }

	public string? ClickySlug { get; init; }

	public string? JobId { get; init; }

	public string? ThreadId { get; init; }

	public required string Title { get; init; }

	public string? Summary { get; init; }

	public NotchUpdateRequest? Request { get; init; }

	public NotchUpdateReadyOutput? ReadyOutput { get; init; }

	public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();

	public bool IsStopped { get; init; }

	public string? RoutineBadge { get; init; }

	public string? RoutineTitle { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private NotchUpdateContent(NotchUpdateContent original)
	{
		RailKey = original.RailKey;
		ClickySlug = original.ClickySlug;
		JobId = original.JobId;
		ThreadId = original.ThreadId;
		Title = original.Title;
		Summary = original.Summary;
		Request = original.Request;
		ReadyOutput = original.ReadyOutput;
		Files = original.Files;
		IsStopped = original.IsStopped;
		RoutineBadge = original.RoutineBadge;
		RoutineTitle = original.RoutineTitle;
	}

	public NotchUpdateContent()
	{
	}
}
