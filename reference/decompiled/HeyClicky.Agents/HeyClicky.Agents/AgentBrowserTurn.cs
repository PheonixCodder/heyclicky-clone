using System;

namespace HeyClicky.Agents;

public sealed record AgentBrowserTurn(string Title, string GroupLabel, string? LatestCommentary, string? ThinkingHeading, Func<bool> IsActive);
