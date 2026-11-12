namespace HeyClicky.Core.Proactive;

public static class ProactiveLanePause
{
	public const bool IsPausedForRelease = true;

	public static bool IsLaneEnabled(bool storedPreference, bool isPaused = true)
	{
		return !isPaused && storedPreference;
	}
}
