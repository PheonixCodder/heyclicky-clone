namespace HeyClicky;

public enum DictationInsertOutcome
{
	Inserted,
	RefusedUnsafeTarget,
	FocusLost,
	ModifiersHeld,
	ClipboardWriteFailed,
	EmptyTranscript
}
