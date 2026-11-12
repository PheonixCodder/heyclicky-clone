namespace HeyClicky.Core.Proactive;

public sealed record IntegrationCandidate(string ToolkitSlug, string DisplayName, string SourceApplicationName, string? SourceHost);
