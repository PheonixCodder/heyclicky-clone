using System;

namespace HeyClicky.App.Dock;

public static class DeckFocus
{
	public static int Clamped(int? focus, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		return Math.Min(Math.Max(focus ?? ((count - 1) / 2), 0), count - 1);
	}
}
