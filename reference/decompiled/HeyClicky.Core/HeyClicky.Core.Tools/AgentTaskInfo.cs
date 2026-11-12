using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Tools;

public sealed record AgentTaskInfo(string TaskId, string? ThreadId, string Title, AgentTaskStatus Status, string? ClickySlug, string? ClickyName, string ProgressSummary, string? Summary, IReadOnlyList<string> Artifacts, bool RequiresExtraUsageApproval, IReadOnlyList<string> UserPrompts, IReadOnlyList<string> Commentary, string StreamingResponse, string? WorkspacePath, DateTimeOffset StartedAt)
{
	public bool IsWorking
	{
		get
		{
			AgentTaskStatus status = Status;
			if ((uint)status <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public string? PendingComputerUseRequest { get; init; }

	public bool IsDemo { get; init; }
}
