using System.Collections.Generic;
using HeyClicky.Drawing.Audio;

namespace HeyClicky.Drawing;

public sealed class LessonPlaybackSounds
{
	internal readonly List<SoundItem> Items;

	public const int SampleRate = 48000;

	public int Count => Items.Count;

	internal LessonPlaybackSounds(List<SoundItem> items)
	{
		Items = items;
	}

	public float[] Mix(double seconds)
	{
		return SoundPlayer.Shared.MixOffline(Items, 0.0, seconds);
	}
}
