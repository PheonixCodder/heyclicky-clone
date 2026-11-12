namespace HeyClicky;

public sealed record ClickTargetRing(PointD CenterPx, double RadiusPx, RectD? ControlFramePx, string? Label);
