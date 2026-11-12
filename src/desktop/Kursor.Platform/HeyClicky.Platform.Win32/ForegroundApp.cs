namespace HeyClicky.Platform.Win32;

public sealed record ForegroundApp(nint WindowHandle, int ProcessId, string ProcessName, string? ExecutablePath, string AppName, string WindowTitle, string? BrowserUrl);
