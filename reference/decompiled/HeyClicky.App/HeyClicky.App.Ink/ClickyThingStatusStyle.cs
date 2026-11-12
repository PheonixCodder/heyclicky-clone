using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyThingStatusStyle
{
	public static string RawValue(this ClickyThingStatus status)
	{
		if (status == ClickyThingStatus.NeedsYou)
		{
			return "needsYou";
		}
		return status.ToString().ToLowerInvariant();
	}

	public static Color Base(this ClickyThingStatus status)
	{
		string hex;
		switch (status)
		{
		case ClickyThingStatus.NeedsYou:
			hex = "#F5A524";
			break;
		case ClickyThingStatus.Working:
		case ClickyThingStatus.Watching:
			hex = "#2F7CF6";
			break;
		case ClickyThingStatus.Ready:
			hex = "#1F9D57";
			break;
		default:
			hex = "#6B7285";
			break;
		}
		return PaperColor.Hex(hex);
	}

	public static Color StampInk(this ClickyThingStatus status)
	{
		string hex;
		switch (status)
		{
		case ClickyThingStatus.NeedsYou:
			hex = "#B85A06";
			break;
		case ClickyThingStatus.Working:
		case ClickyThingStatus.Watching:
			hex = "#2D5BE3";
			break;
		case ClickyThingStatus.Ready:
			hex = "#1F8A4C";
			break;
		default:
			hex = "#6B7285";
			break;
		}
		return PaperColor.Hex(hex);
	}

	public static Color OnBlack(this ClickyThingStatus status)
	{
		string hex;
		switch (status)
		{
		case ClickyThingStatus.NeedsYou:
			hex = "#FFB547";
			break;
		case ClickyThingStatus.Working:
		case ClickyThingStatus.Watching:
			hex = "#8EC5FF";
			break;
		case ClickyThingStatus.Ready:
			hex = "#5BD08A";
			break;
		default:
			hex = "#9093A0";
			break;
		}
		return PaperColor.Hex(hex);
	}

	public static string StampWord(this ClickyThingStatus status)
	{
		return status switch
		{
			ClickyThingStatus.NeedsYou => "NEEDS YOU", 
			ClickyThingStatus.Working => "WRITING", 
			ClickyThingStatus.Watching => "WATCHING", 
			ClickyThingStatus.Ready => "READY", 
			ClickyThingStatus.Done => "DONE", 
			_ => "PAUSED", 
		};
	}
}
