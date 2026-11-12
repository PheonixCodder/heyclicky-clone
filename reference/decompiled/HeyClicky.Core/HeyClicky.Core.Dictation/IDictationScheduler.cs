using System;

namespace HeyClicky.Core.Dictation;

public interface IDictationScheduler
{
	DateTimeOffset Now { get; }

	IDisposable Schedule(TimeSpan delay, Action action);
}
