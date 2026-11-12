using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Notch;

public sealed record ChatHandOff
{
	public required string Id { get; set; }

	public required ChatHandOffKind Kind { get; set; }

	public string? JobId { get; set; }

	public string? ClickySlug { get; set; }

	public required string Title { get; set; }

	public required DateTimeOffset CreatedAt { get; set; }

	public bool IsFollowUp { get; set; }

	public bool IsSentFromWindow { get; set; }

	public ChatHandOffOutcome? Outcome { get; set; }

	public string? ReplyText { get; set; }

	public string? LessonId { get; set; }

	public string? DeliveredText { get; set; }

	public string? DeliveredToApp { get; set; }

	public bool? WasCopied { get; set; }

	public string? FullReply { get; set; }

	public IReadOnlyList<string>? ArtifactEntries { get; set; }

	public JsonObject? Answer { get; set; }

	public string? ThreadId { get; set; }

	public JsonObject? Card { get; set; }

	public bool? IsRoutineRun { get; set; }

	public string? TaskTitle { get; set; }

	public IReadOnlyList<string>? FollowUps { get; set; }

	public string? SentText { get; set; }

	public IReadOnlyList<string>? SentFiles { get; set; }

	public IReadOnlyList<string>? SitesRead { get; set; }

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
