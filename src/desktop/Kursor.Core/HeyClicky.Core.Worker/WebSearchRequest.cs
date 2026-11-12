namespace HeyClicky.Core.Worker;

public sealed record WebSearchRequest(string Query, int? Count = null, string? Freshness = null);
