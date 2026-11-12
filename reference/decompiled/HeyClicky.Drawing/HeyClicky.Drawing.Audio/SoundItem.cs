using System.Numerics;

namespace HeyClicky.Drawing.Audio;

internal readonly record struct SoundItem(bool IsStroke, string? Texture, string? SoundId, double BeginTime, double Duration, double LengthInPoints, Vector4 Timing, float Gain)
{
	public static SoundItem Stroke(string texture, double beginTime, double duration, double lengthInPoints, Vector4 timing)
	{
		return new SoundItem(IsStroke: true, texture, null, beginTime, duration, lengthInPoints, timing, 1f);
	}

	public static SoundItem Event(string soundId, double beginTime, float gain)
	{
		return new SoundItem(IsStroke: false, null, soundId, beginTime, 0.0, 0.0, default(Vector4), gain);
	}
}
