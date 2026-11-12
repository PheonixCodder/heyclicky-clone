using System;
using Windows.Win32;
using Windows.Win32.UI.HiDpi;

namespace HeyClicky.Platform.Win32;

internal readonly struct DpiAwarenessScope : IDisposable
{
	private readonly DPI_AWARENESS_CONTEXT previousContext;

	private DpiAwarenessScope(DPI_AWARENESS_CONTEXT previous)
	{
		previousContext = previous;
	}

	public static DpiAwarenessScope PerMonitorV2()
	{
		return new DpiAwarenessScope(PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2));
	}

	public void Dispose()
	{
		if (previousContext != default(DPI_AWARENESS_CONTEXT))
		{
			PInvoke.SetThreadDpiAwarenessContext(previousContext);
		}
	}
}
