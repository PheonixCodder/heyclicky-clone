using System;
using System.Collections.Generic;

namespace HeyClicky.App.DevFakes;

public sealed class FakeHighlightOverlay : IHighlightOverlay, IDisposable
{
	public bool IsEnabled { get; set; } = true;

	public void BeginWatch(string colorHex)
	{
	}

	public IReadOnlyList<HighlightStroke> EndWatch()
	{
		return Array.Empty<HighlightStroke>();
	}

	public void Dispose()
	{
	}
}
