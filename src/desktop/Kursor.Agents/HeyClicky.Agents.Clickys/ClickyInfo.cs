using System;

namespace HeyClicky.Agents.Clickys;

public sealed record ClickyInfo(string Slug, string Name, string RoleText, string OneLiner, string? ThreadId, DateTimeOffset CreatedAt, bool IsArchived = false);
