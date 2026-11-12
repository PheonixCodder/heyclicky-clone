using System.Collections.Generic;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents.Harness;

public abstract record AgentHarnessEvent
{
	public sealed record TurnStats(IReadOnlyDictionary<string, double> Stats) : AgentHarnessEvent();

	public sealed record Commentary(string Text) : AgentHarnessEvent();

	public sealed record Thinking(string Heading) : AgentHarnessEvent();

	public sealed record Plan(IReadOnlyList<(string Text, string Status)> Steps) : AgentHarnessEvent();

	public sealed record ToolStarted(string Id, string Tool, string? Server, string? Command) : AgentHarnessEvent();

	public sealed record ToolFinished(string Id, string Tool, string? Server, string? Command) : AgentHarnessEvent();

	public sealed record WebSearch(string Id, string Query, string? Url) : AgentHarnessEvent();

	public sealed record ProgramBatch(ClickyProgramBatch Batch) : AgentHarnessEvent();

	public sealed record AnswerDelta(string Text) : AgentHarnessEvent();

	public sealed record Finished(string Status, string? Error) : AgentHarnessEvent();

	public sealed record NotePreview(string Path, string Text) : AgentHarnessEvent();

	public sealed record NoteSaved(string Path) : AgentHarnessEvent();

	public sealed record ConnectedAppsUnreachable : AgentHarnessEvent;

	public sealed record Compacted : AgentHarnessEvent;
}
