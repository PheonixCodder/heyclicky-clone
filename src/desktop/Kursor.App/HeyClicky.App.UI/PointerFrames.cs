using System.Collections.Generic;
using Microsoft.UI.Xaml.Input;

namespace HeyClicky.App.UI;

public static class PointerFrames
{
	private static readonly Dictionary<uint, uint> newest = new Dictionary<uint, uint>();

	public static void Note(PointerRoutedEventArgs e)
	{
		uint pointerId = e.Pointer.PointerId;
		uint frameId = e.GetCurrentPoint(null).FrameId;
		if (!newest.TryGetValue(pointerId, out var value) || frameId > value)
		{
			newest[pointerId] = frameId;
		}
	}

	public static bool IsStale(PointerRoutedEventArgs e)
	{
		uint frameId = e.GetCurrentPoint(null).FrameId;
		if (newest.TryGetValue(e.Pointer.PointerId, out var value))
		{
			return frameId < value;
		}
		return false;
	}
}
