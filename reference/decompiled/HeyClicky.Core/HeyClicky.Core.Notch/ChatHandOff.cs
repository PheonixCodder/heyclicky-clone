using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Notch;

public sealed record ChatHandOff
{
	public required string Id { get; init; }

	public required ChatHandOffKind Kind { get; init; }

	public string? JobId { get; init; }

	public string? ClickySlug { get; init; }

	public required string Title { get; init; }

	public required DateTimeOffset CreatedAt { get; init; }

	public bool IsFollowUp { get; init; }

	public bool IsSentFromWindow { get; init; }

	public ChatHandOffOutcome? Outcome { get; init; }

	public string? ReplyText { get; init; }

	public string? LessonId { get; init; }

	public string? DeliveredText { get; init; }

	public string? DeliveredToApp { get; init; }

	public bool? WasCopied { get; init; }

	public string? FullReply { get; init; }

	public IReadOnlyList<string>? ArtifactEntries { get; init; }

	public JsonObject? Answer { get; init; }

	public string? ThreadId { get; init; }

	public JsonObject? Card { get; init; }

	public bool? IsRoutineRun { get; init; }

	public string? TaskTitle { get; init; }

	public IReadOnlyList<string>? FollowUps { get; init; }

	public string? SentText { get; init; }

	public IReadOnlyList<string>? SentFiles { get; init; }

	public IReadOnlyList<string>? SitesRead { get; init; }

	[JsonIgnore]
	public string TargetKey => Kind switch
	{
		ChatHandOffKind.Drawing => "drawing:" + (LessonId ?? Id), 
		ChatHandOffKind.Text => "text:" + Id, 
		_ => (JobId != null) ? ("job:" + JobId) : ("clicky:" + ClickySlug), 
	};

	public static string TargetKeyFor(string? jobId, string? clickySlug)
	{
		if (jobId == null)
		{
			return "clicky:" + clickySlug;
		}
		return "job:" + jobId;
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ChatHandOff(ChatHandOff original)
	{
		Id = original.Id;
		Kind = original.Kind;
		JobId = original.JobId;
		ClickySlug = original.ClickySlug;
		Title = original.Title;
		CreatedAt = original.CreatedAt;
		IsFollowUp = original.IsFollowUp;
		IsSentFromWindow = original.IsSentFromWindow;
		Outcome = original.Outcome;
		ReplyText = original.ReplyText;
		LessonId = original.LessonId;
		DeliveredText = original.DeliveredText;
		DeliveredToApp = original.DeliveredToApp;
		WasCopied = original.WasCopied;
		FullReply = original.FullReply;
		ArtifactEntries = original.ArtifactEntries;
		Answer = original.Answer;
		ThreadId = original.ThreadId;
		Card = original.Card;
		IsRoutineRun = original.IsRoutineRun;
		TaskTitle = original.TaskTitle;
		FollowUps = original.FollowUps;
		SentText = original.SentText;
		SentFiles = original.SentFiles;
		SitesRead = original.SitesRead;
	}

	public ChatHandOff()
	{
	}
}
