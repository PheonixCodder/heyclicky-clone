namespace HeyClicky.Core.Tools;

public sealed record AgentClickyInfo(string Slug, string Name, string RoleText, string OneLiner, string? ThreadId, bool IsArchived = false);
