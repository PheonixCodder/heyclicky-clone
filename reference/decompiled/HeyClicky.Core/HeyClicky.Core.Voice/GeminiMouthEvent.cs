namespace HeyClicky.Core.Voice;

public readonly record struct GeminiMouthEvent(GeminiMouthEventKind Kind, string? Base64Audio = null, string? UsageJson = null);
