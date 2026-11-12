using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class FitDecision
{
	private const long SettleMilliseconds = 300L;

	private Size? size;

	private UIElement? first;

	private UIElement? last;

	private int count = -1;

	private int previous = -1;

	private long changedAt = -4611686018427387904L;

	public int FirstCandidate(Size available, UIElementCollection children, int chosen)
	{
		if (chosen < 0 || chosen >= children.Count || count != children.Count || (object)first != children[0] || (object)last != children[children.Count - 1])
		{
			previous = -1;
			return 0;
		}
		bool flag = Environment.TickCount64 - changedAt < 300;
		if (!(size == available || flag))
		{
			return 0;
		}
		return chosen;
	}

	public void Remember(Size available, UIElementCollection children, int chosen)
	{
		if (previous >= 0 && chosen != previous)
		{
			changedAt = Environment.TickCount64;
		}
		previous = chosen;
		size = available;
		count = children.Count;
		first = ((count > 0) ? children[0] : null);
		last = ((count > 0) ? children[count - 1] : null);
	}
}
