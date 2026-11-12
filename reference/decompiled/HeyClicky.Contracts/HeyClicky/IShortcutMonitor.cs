using System;

namespace HeyClicky;

public interface IShortcutMonitor : IDisposable
{
	bool IsPushToTalkHeld { get; }

	event EventHandler<ShortcutAction>? ShortcutTriggered;

	void Start();

	void Stop();
}
