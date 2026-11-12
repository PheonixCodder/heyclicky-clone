using System;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyLearnedHowTo(string Text, DateTimeOffset LearnedAt, string? JobId);
