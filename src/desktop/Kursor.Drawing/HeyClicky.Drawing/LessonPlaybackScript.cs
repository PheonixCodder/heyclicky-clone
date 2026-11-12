using System.Collections.Generic;

namespace HeyClicky.Drawing;

public sealed record LessonPlaybackScript(string Program, byte[] ScreenJpeg, int CanvasWidth, int CanvasHeight, double PointWidth, double DisplayScale, IReadOnlyList<(double Start, double? Length)> Lines, double Duration, string InkHex);
