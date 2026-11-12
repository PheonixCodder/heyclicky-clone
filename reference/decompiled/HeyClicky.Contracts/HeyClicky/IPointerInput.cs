using System;

namespace HeyClicky;

public interface IPointerInput
{
	PointD CursorPosition { get; }

	bool IsAnyButtonDown { get; }

	IDisposable WatchPresses(Action<PointD> onPress);
}
