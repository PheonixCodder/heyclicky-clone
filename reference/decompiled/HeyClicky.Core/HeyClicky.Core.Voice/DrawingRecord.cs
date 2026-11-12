using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Voice;

public sealed record DrawingRecord(string Question, string Title, string Program, IReadOnlyList<Line> Lines, double Duration, ScreenCaptureFrame Screen, string CursorColorHex, DateTimeOffset At, byte[]? Narration = null, int NarrationSampleRate = 0)
{
	public sealed record Line(string Text, double Start, double? Length);

	public bool KeepsInChat { get; init; }
}
