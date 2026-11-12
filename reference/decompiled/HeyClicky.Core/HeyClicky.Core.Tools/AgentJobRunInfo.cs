using System;

namespace HeyClicky.Core.Tools;

public sealed record AgentJobRunInfo(string Ask, DateTimeOffset StartedAt, string Outcome, string? Summary);
