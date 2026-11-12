namespace HeyClicky.Core.Voice;

public sealed record CompanionTurnMetrics(bool Typed, int TranscriptMs, int CaptureMs, int? BrainFirstByteMs, int? FirstSayLineMs, int? FirstAudioMs, int? FirstSpokenMs, int TotalMs, string? Model);
