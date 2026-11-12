using System.Collections.Generic;

namespace HeyClicky.Platform.Overlay;

internal interface IOverlayLayer
{
	int IdleWakeIntervalMs { get; }

	bool WantsMouseMoveWake { get; }

	bool OnFrame(in OverlayFrame frame);

	bool OnIdleWake(in OverlayFrame frame);

	void OnDisplaysChanged(IReadOnlyList<DisplayInfo> displays, RectD virtualScreenPx);
}
