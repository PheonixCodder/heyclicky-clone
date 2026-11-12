namespace HeyClicky.Platform.Capture;

public sealed record ScreenCaptureTimings(double TotalMs, double GpuMs, double EncodeMs, string Backend, int DisplayCount);
