namespace HeyClicky.Core.Notch;

public static class ChatWorkCardRules
{
	public static ChatWorkCardMode Mode(bool isWorking, bool isFinalizing, bool hasAskOfTheUser, bool hidesFinalizing)
	{
		if (isWorking && !(hidesFinalizing && isFinalizing))
		{
			return ChatWorkCardMode.Working;
		}
		if (!hasAskOfTheUser)
		{
			return ChatWorkCardMode.Hidden;
		}
		return ChatWorkCardMode.Ask;
	}
}
