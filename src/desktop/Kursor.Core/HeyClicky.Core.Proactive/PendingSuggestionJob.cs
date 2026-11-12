using System;

namespace HeyClicky.Core.Proactive;

public sealed record PendingSuggestionJob(string JobId, DateTimeOffset StartedAt, ProactiveCheckReason CheckReason);
