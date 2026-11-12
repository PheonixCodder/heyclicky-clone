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
	public string RailKey { get; set; } = "";

	public string Title { get; set; } = "";

	public ArcCardState State { get; set; }

	public ArcPaper Paper { get; set; }

	public ClickyArcPlan? Plan { get; set; }

	public string? CurrentStepLine { get; set; }

	public string? Summary { get; set; }

	public IReadOnlyList<string> Files { get; set; } = Array.Empty<string>();

	public string? ThreadId { get; set; }

	public string? ClickySlug { get; set; }

	public string? JobId { get; set; }

	public string? TaskId { get; set; }

	public string? WorkspacePath { get; set; }

	public bool TaskIsRunning { get; set; }

	public CardFooterRequest? Request { get; set; }

	public CardReadyOutput? ReadyOutput { get; set; }

	public ClickyLiveLine? LiveLine { get; set; }

	public ClickyAnswer? Answer { get; set; }

	public bool IsFinalizing { get; set; }

	public string? NotePath { get; set; }

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
