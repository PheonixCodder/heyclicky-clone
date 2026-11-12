namespace HeyClicky.Core.Voice;

public sealed record MouthConfig(string VoiceIdentifier, double Speed, string UsageContext, string? Language = null);
