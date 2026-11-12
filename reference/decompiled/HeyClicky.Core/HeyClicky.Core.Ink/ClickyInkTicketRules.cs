namespace HeyClicky.Core.Ink;

public static class ClickyInkTicketRules
{
	public static string Code(string text)
	{
		int num = text.IndexOf('(');
		int num2 = text.IndexOf(')');
		if (num >= 0 && num2 >= 0 && num < num2)
		{
			int num3 = num + 1;
			return text.Substring(num3, num2 - num3).ToUpperInvariant();
		}
		if (text.CharacterCount() <= 4)
		{
			return text.ToUpperInvariant();
		}
		return text.PrefixCharacters(3).ToUpperInvariant();
	}

	public static string Place(string text)
	{
		int num = text.IndexOf('(');
		if (num >= 0)
		{
			return text.Substring(0, num).TrimWs();
		}
		if (text.CharacterCount() > 4)
		{
			return text;
		}
		return "";
	}

	public static string Icon(string? kind, bool hasRoute)
	{
		switch (kind?.ToLowerInvariant())
		{
		case "train":
			return "tram.fill";
		case "event":
		case "show":
		case "concert":
		case "game":
			return "ticket.fill";
		case "bus":
			return "bus.fill";
		default:
			return hasRoute ? "airplane" : "ticket.fill";
		}
	}
}
