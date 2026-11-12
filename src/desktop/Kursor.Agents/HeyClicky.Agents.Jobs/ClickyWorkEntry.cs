using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyWorkEntry
{
	public required string Id { get; init; }

	public required string Title { get; init; }

	public DateTimeOffset LastActive { get; init; }

	public IReadOnlyList<string> Asks { get; init; } = Array.Empty<string>();

	public string? Result { get; init; }

	public required string State { get; init; }

	public string? FolderPath { get; init; }

	public bool IsEarlierClicky { get; init; }

	public string? ReportText { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyWorkEntry(ClickyWorkEntry original)
	{
		Id = original.Id;
		Title = original.Title;
		LastActive = original.LastActive;
		Asks = original.Asks;
		Result = original.Result;
		State = original.State;
		FolderPath = original.FolderPath;
		IsEarlierClicky = original.IsEarlierClicky;
		ReportText = original.ReportText;
	}

	public ClickyWorkEntry()
	{
	}
}
