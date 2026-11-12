using System.Collections.Generic;

namespace HeyClicky.Drawing.Painter;

internal sealed record MarkTiming(int Mark, double BeginTime, double EndTime, IReadOnlyList<PenStroke> PenStrokes);
