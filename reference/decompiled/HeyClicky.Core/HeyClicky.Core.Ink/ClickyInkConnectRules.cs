namespace HeyClicky.Core.Ink;

public static class ClickyInkConnectRules
{
	public static string Title(ClickyInkConnections.Phase? phase, string appName, bool isAlreadyConnected, bool needsReconnect)
	{
		if (phase is ClickyInkConnections.Phase.Connected || isAlreadyConnected)
		{
			return appName + " is connected";
		}
		if (!needsReconnect)
		{
			return "Connect " + appName;
		}
		return "Reconnect " + appName;
	}

	public static string Label(ClickyInkConnections.Phase? phase, bool isAlreadyConnected)
	{
		if (!(phase is ClickyInkConnections.Phase.Connected || isAlreadyConnected))
		{
			return "access needed";
		}
		return "access granted";
	}

	public static bool ShowsConnectedStamp(ClickyInkConnections.Phase? phase, bool isAlreadyConnected)
	{
		if (!(phase is ClickyInkConnections.Phase.Connected))
		{
			return (object)phase == null && isAlreadyConnected;
		}
		return true;
	}
}
