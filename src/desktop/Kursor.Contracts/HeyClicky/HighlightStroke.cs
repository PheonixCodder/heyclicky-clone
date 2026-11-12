using System.Collections.Generic;

namespace HeyClicky;

public sealed record HighlightStroke(IReadOnlyList<PointD> PointsPx, IReadOnlyList<PointD> DwellsPx, int DisplayIndex);
