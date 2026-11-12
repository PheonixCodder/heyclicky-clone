namespace HeyClicky.Platform.Win32;

public sealed record UiElementInfo(string Name, string ControlType, string? AutomationId, string? ClassName, RectD BoundsPx, int ProcessId, bool IsPassword, bool IsInvokable);
