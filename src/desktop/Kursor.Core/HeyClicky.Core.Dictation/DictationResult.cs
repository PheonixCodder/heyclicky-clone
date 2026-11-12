namespace HeyClicky.Core.Dictation;

public sealed record DictationResult(string Text, DictationDeliveryOutcome Outcome, string? ClipboardReason, bool UsedBatch, bool HandsFree, bool UsedWarmConnection, int ReleaseToInsertMs);
