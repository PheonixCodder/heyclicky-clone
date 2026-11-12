namespace HeyClicky.Core.Worker;

public sealed record UserMemoryResponse(string? Markdown, string? ProfileMarkdown, string? ContextMarkdown, bool? HasProfile, string? UpdatedAt);
