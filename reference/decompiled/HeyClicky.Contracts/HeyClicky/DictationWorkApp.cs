namespace HeyClicky;

public sealed record DictationWorkApp(int ProcessId, nint WindowHandle, string AppName, string? SiteHost, bool AllowsBlindPaste);
