namespace HeyClicky.Core.Brain;

public sealed record BrainTurnContext(string SessionId, bool IsSessionsFirstTurn, bool SendEnvironment, string? DrawingCodeToResend);
