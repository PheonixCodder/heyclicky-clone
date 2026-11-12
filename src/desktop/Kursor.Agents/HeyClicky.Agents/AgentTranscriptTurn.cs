using System;
using System.Collections.Generic;

namespace HeyClicky.Agents;

public sealed record AgentTranscriptTurn(string? TurnId, IReadOnlyList<string> UserMessages, IReadOnlyList<string> WorkSteps, IReadOnlyList<string> AssistantMessages, IReadOnlyList<string> NextActions, IReadOnlyList<string> Artifacts, string Status, string? ErrorMessage, DateTimeOffset? StartedAt = null, DateTimeOffset? FinishedAt = null);
