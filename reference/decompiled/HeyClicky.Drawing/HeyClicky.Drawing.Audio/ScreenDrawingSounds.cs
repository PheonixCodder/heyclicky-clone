using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing.Audio;

public static class ScreenDrawingSounds
{
	public static void Prepare()
	{
		SoundPlayer.Shared.Prepare();
	}

	public static void Play(string soundId, float gain, double delay, object owner)
	{
		SoundPlayer shared = SoundPlayer.Shared;
		int num = 1;
		List<SoundItem> list = new List<SoundItem>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<SoundItem> span = CollectionsMarshal.AsSpan(list);
		int index = 0;
		span[index] = SoundItem.Event(soundId, DrawingThread.RealNow + delay, gain);
		shared.Schedule(list, owner);
	}
}
