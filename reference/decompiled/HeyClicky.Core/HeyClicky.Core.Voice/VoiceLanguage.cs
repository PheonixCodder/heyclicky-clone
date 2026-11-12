namespace HeyClicky.Core.Voice;

public sealed record VoiceLanguage(string Code, string Name)
{
	public string? NativeName => VoiceCatalog.NativeName(Code);
}
