using System;
using System.Collections.Generic;

namespace HeyClicky;

public interface IHighlightOverlay : IDisposable
{
	bool IsEnabled { get; set; }

	void BeginWatch(string colorHex);

	IReadOnlyList<HighlightStroke> EndWatch();
}
