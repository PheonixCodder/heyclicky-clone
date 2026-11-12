using System;
using System.Collections.Generic;
using HeyClicky.Platform.Overlay;

namespace HeyClicky.App.Wiring;

public sealed class DeferredHighlightOverlay : IHighlightOverlay, IDisposable
{
	private volatile HighlightOverlay? inner;

	private bool enabled = true;

	public HighlightOverlay? Inner => inner;

	public bool IsEnabled
	{
		get
		{
			return inner?.IsEnabled ?? enabled;
		}
		set
		{
			enabled = value;
			HighlightOverlay highlightOverlay = inner;
			if (highlightOverlay != null)
			{
				highlightOverlay.IsEnabled = value;
			}
		}
	}

	public void Attach(HighlightOverlay real)
	{
		real.IsEnabled = enabled;
		inner = real;
	}

	public void BeginWatch(string colorHex)
	{
		inner?.BeginWatch(colorHex);
	}

	public IReadOnlyList<HighlightStroke> EndWatch()
	{
		return inner?.EndWatch() ?? Array.Empty<HighlightStroke>();
	}

	public void Clear()
	{
		inner?.Clear();
	}

	public void Dispose()
	{
		inner?.Dispose();
	}
}
