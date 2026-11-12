namespace HeyClicky.Core.Voice;

public static class FullscreenGameNote
{
	public static string? Text()
	{
		nint num = FullscreenGuard.GameWindow();
		if (num == 0)
		{
			return null;
		}
		string text = FullscreenGuard.ProcessName(num);
		return "Screen: " + text + " is a game in exclusive fullscreen, so nothing you draw, point at or highlight can be seen over it (HeyClicky stays out of its way so it keeps fullscreen), and a circle the user draws doesn't show either. Your voice works and you still see the screen: answer by voice. If the user asks you to point, draw or circle something, or wonders why nothing shows, tell them once, in a short sentence, to switch " + text + "'s window mode to Borderless in its video settings so you can draw over it. Don't repeat it every turn.";
	}
}
