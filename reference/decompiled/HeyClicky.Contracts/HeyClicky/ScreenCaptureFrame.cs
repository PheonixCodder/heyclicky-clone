namespace HeyClicky;

public sealed record ScreenCaptureFrame(DisplayInfo Display, byte[] Jpeg, int PixelWidth, int PixelHeight, bool ContainsCursor, PointD CursorInImage);
