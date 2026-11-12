using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Notch;

public sealed record ChatTaskEnd
{
	public string? JobId { get; set; }

	public string? ClickySlug { get; set; }

	public string? ThreadId { get; set; }

	public bool IsDemo { get; set; }

	public string? PendingComputerUseRequest { get; set; }

	public string? PendingUserStep { get; set; }

	public bool NeedsChromeConnection { get; set; }

	public string? FinalSummary { get; set; }

	public string? FinalResponseMarkdown { get; set; }

	public IReadOnlyList<string> CompletedAssistantMessages { get; set; } = Array.Empty<string>();

	public IReadOnlyList<string> UserFacingArtifactPaths { get; set; } = Array.Empty<string>();

	public JsonObject? FinalAnswer { get; set; }

	public IReadOnlyList<string> SuggestedNextActions { get; set; } = Array.Empty<string>();

	public bool IsScheduledRoutineRun { get; set; }

	public string? CompletedThreadName { get; set; }

	public IReadOnlyList<string> SitesRead { get; set; } = Array.Empty<string>();
}
