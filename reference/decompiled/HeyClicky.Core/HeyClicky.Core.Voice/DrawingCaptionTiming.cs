using System;

namespace HeyClicky.Core.Voice;

public static class DrawingCaptionTiming
{
	public static double RevealSeconds(string line, double readingSeconds)
	{
		int num = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
		return Math.Min(readingSeconds * 0.8, Math.Max(0.6, (double)num * 0.24));
	}

	public static double SpokenSeconds(string line, double speed)
	{
		return Math.Max(0.8, (double)line.Length / (14.5 * Math.Max(0.5, speed)));
	}
}
