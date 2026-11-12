namespace HeyClicky.Platform.Audio;

internal sealed class OutputOpenFailures
{
	public const int BeforeOutputIsIrrecoverable = 3;

	private bool reportedThisStreak;

	public int ConsecutiveFailures { get; private set; }

	public bool IsIrrecoverable => ConsecutiveFailures >= 3;

	public bool DrainsByWallClock(bool hasPendingAudio)
	{
		return IsIrrecoverable && hasPendingAudio;
	}

	public void NoteOpened()
	{
		ConsecutiveFailures = 0;
		reportedThisStreak = false;
	}

	public bool NoteOpenFailed()
	{
		ConsecutiveFailures++;
		if (!IsIrrecoverable || reportedThisStreak)
		{
			return false;
		}
		reportedThisStreak = true;
		return true;
	}

	public void NoteDrainedByWallClock()
	{
		if (IsIrrecoverable)
		{
			ConsecutiveFailures = 2;
		}
	}

	public void NoteDeviceChanged()
	{
		NoteOpened();
	}
}
