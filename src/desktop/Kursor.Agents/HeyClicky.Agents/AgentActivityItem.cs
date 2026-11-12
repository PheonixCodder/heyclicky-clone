using System;

namespace HeyClicky.Agents;

public sealed record AgentActivityItem(string Id, AgentActivityKind Kind, string Text, bool? IsRunning, DateTimeOffset At);
