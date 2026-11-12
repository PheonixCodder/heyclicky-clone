using System.Collections.Generic;

namespace HeyClicky;

public sealed record PenStrokePath(IReadOnlyList<PointD> PointsPx, double BeginTime, double Duration, double X1, double Y1, double X2, double Y2);
