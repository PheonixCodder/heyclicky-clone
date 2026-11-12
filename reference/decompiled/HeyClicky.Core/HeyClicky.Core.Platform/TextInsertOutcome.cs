namespace HeyClicky.Core.Platform;

public sealed record TextInsertOutcome(string Status, string? Reason = null, string? App = null);
