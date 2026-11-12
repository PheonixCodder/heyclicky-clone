namespace HeyClicky;

public sealed record DictationFocus(object Anchor, int ProcessId, nint WindowHandle, string AppName, string? SiteHost, string? Role, bool IsSecure, bool IsSingleLine, string BeforeText, string SelectedText, string AfterText)
{
	public bool IsEditableInsertionTarget => !IsSecure;
}
