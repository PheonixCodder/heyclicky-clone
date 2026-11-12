namespace HeyClicky.Core.Proactive;

public sealed record ForegroundActivity(string ProcessName, string AppName, bool IsBrowser, string? Host, string? Path, string? WindowTitle);
