namespace HeyClicky.Agents.Runtime;

public static class ComposioTurnErrors
{
	public static bool IsListingFailure(string? error)
	{
		string text = error?.ToLowerInvariant();
		if (text != null && text.Contains("composio"))
		{
			if (!text.Contains("tools/list failed"))
			{
				return text.Contains("initialize failed");
			}
			return true;
		}
		return false;
	}
}
