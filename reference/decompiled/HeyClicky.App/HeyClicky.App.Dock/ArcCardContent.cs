using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public sealed record ArcCardContent
{
	public required string RailKey { get; init; }

	public required string Title { get; init; }

	public required ArcCardState State { get; init; }

	public required ArcPaper Paper { get; init; }

	public ClickyArcPlan? Plan { get; init; }

	public string? CurrentStepLine { get; init; }

	public string? Summary { get; init; }

	public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();

	public string? ThreadId { get; init; }

	public string? ClickySlug { get; init; }

	public string? JobId { get; init; }

	public string? TaskId { get; init; }

	public string? WorkspacePath { get; init; }

	public bool TaskIsRunning { get; init; }

	public CardFooterRequest? Request { get; init; }

	public CardReadyOutput? ReadyOutput { get; init; }

	public ClickyLiveLine? LiveLine { get; init; }

	public ClickyAnswer? Answer { get; init; }

	public bool IsFinalizing { get; init; }

	public string? NotePath { get; init; }

	public bool IsWorking => ArcCardStates.IsWorking(State);

	public bool IsDone => ArcCardStates.IsDone(State);

	public string Shape
	{
		get
		{
			_003C_003Ey__InlineArray15<object> buffer = default(_003C_003Ey__InlineArray15<object>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 0) = State;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 1) = IsFinalizing;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 2) = Title;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 3) = Summary;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 4) = string.Join(",", Files);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 5) = Request?.Kind;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 6) = Request?.Text;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 7) = ReadyOutput?.Kind;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 8) = ReadyOutput?.ButtonTitle;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 9) = (((object)Answer == null) ? "" : JsonSerializer.Serialize(Answer));
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 10) = TaskIsRunning;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 11) = (((object)Plan == null) ? "-" : string.Join(",", Plan.Steps.Select((ClickyArcPlan.Step s) => s.Text)));
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 12) = (((object)Plan == null || Plan.IsEmpty) ? "noplan" : "plan");
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 13) = (((object)LiveLine == null) ? "noline" : "line");
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray15<object>, object>(ref buffer, 14) = ((CurrentStepLine == null) ? "nostep" : "step");
			return string.Join("|", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray15<object>, object>(in buffer, 15));
		}
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ArcCardContent(ArcCardContent original)
	{
		RailKey = original.RailKey;
		Title = original.Title;
		State = original.State;
		Paper = original.Paper;
		Plan = original.Plan;
		CurrentStepLine = original.CurrentStepLine;
		Summary = original.Summary;
		Files = original.Files;
		ThreadId = original.ThreadId;
		ClickySlug = original.ClickySlug;
		JobId = original.JobId;
		TaskId = original.TaskId;
		WorkspacePath = original.WorkspacePath;
		TaskIsRunning = original.TaskIsRunning;
		Request = original.Request;
		ReadyOutput = original.ReadyOutput;
		LiveLine = original.LiveLine;
		Answer = original.Answer;
		IsFinalizing = original.IsFinalizing;
		NotePath = original.NotePath;
	}

	public ArcCardContent()
	{
	}
}
