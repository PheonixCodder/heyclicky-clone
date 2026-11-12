using System;

namespace HeyClicky.Core.Notch;

public sealed class MultiAgentRotationCursor
{
	public static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(5L);

	private DateTimeOffset lastAdvancedAt = DateTimeOffset.MinValue;

	public int Index { get; private set; }

	public void Restart(DateTimeOffset now)
	{
		lastAdvancedAt = now;
	}

	public bool AdvanceIfHoldElapsed(DateTimeOffset now, int activeAgentCount)
	{
		if (activeAgentCount <= 1)
		{
			return false;
		}
		if (now - lastAdvancedAt < HoldDuration)
		{
			return false;
		}
		lastAdvancedAt = now;
		Index = (Index + 1) % activeAgentCount;
		return true;
	}

	public TimeSpan UntilDue(DateTimeOffset now)
	{
		TimeSpan timeSpan = HoldDuration - (now - lastAdvancedAt);
		if (!(timeSpan < TimeSpan.Zero))
		{
			return timeSpan;
		}
		return TimeSpan.Zero;
	}

	public void Clamp(int activeAgentCount)
	{
		if (activeAgentCount > 0 && Index >= activeAgentCount)
		{
			Index %= activeAgentCount;
		}
	}

	public int IndexFor(int activeAgentCount, bool isMultiAgentPhase)
	{
		if (activeAgentCount > 0 && isMultiAgentPhase)
		{
			return Index % activeAgentCount;
		}
		return 0;
	}
}
