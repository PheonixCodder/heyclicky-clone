using System;

namespace HeyClicky.Agents.Turns;

internal sealed record ActivityRow(string Id, AgentActivityKind Kind, string Text, bool? IsRunning, DateTimeOffset At);
