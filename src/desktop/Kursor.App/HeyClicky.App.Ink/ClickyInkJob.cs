using System;

namespace HeyClicky.App.Ink;

public sealed record ClickyInkJob(string Id, ClickyInkJobPhase Phase, bool IsSeen, DateTimeOffset? FinishedAt, string? ThreadId);
