namespace HeyClicky.Core.Dictation;

public sealed record DictationCleanupContext(string AppName, string? SiteHost, string BeforeText, string SelectedText, string AfterText, bool SingleLine);
