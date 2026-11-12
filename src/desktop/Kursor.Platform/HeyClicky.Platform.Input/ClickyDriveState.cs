namespace HeyClicky.Platform.Input;

public static class ClickyDriveState
{
	private static volatile bool isDriving;

	public static bool IsDrivingNow
	{
		get
		{
			return isDriving;
		}
		set
		{
			isDriving = value;
		}
	}
}
