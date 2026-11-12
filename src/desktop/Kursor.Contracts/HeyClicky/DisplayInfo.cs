namespace HeyClicky;

public sealed record DisplayInfo(int Index, string DeviceName, RectD BoundsPx, RectD WorkAreaPx, double Scale, bool IsPrimary);
