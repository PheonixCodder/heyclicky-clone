using System;

namespace HeyClicky;

public interface INotchPresenter
{
	NotchPresentation Presentation { get; }

	bool IsHovering { get; }

	event EventHandler? PresentationChanged;

	void Present(NotchPresentation presentation);

	void ShowLiveEvent(NotchLiveEvent liveEvent, TimeSpan? autoDismissAfter = null);

	void DismissLiveEvent(Type liveEventType);

	void ReturnToIdle();
}
