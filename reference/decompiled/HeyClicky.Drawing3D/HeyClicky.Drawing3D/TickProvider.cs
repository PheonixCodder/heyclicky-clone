using System;

namespace HeyClicky.Drawing3D;

public delegate ReadOnlySpan<float> TickProvider(string stageId, ReadOnlySpan<TickDue> ticks, double frameSeconds);
