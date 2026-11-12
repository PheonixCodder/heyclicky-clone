namespace HeyClicky.Core.Ink;

public static class ClickyInkStatus
{
	public static ClickyInkStatusState State(string? word)
	{
		switch ((word ?? "").TrimWs().Lowered())
		{
		case "ok":
		case "okay":
		case "good":
		case "green":
		case "up":
		case "fine":
		case "healthy":
		case "on":
		case "live":
		case "clear":
		case "pass":
		case "passing":
		case "done":
		case "yes":
		case "ready":
		case "paid":
		case "on track":
		case "ontrack":
		case "won":
		case "great":
		case "steady":
		case "normal":
			return ClickyInkStatusState.Ok;
		case "watch":
		case "warn":
		case "warning":
		case "amber":
		case "yellow":
		case "orange":
		case "slow":
		case "pending":
		case "risk":
		case "at risk":
		case "caution":
		case "degraded":
		case "soon":
		case "waiting":
		case "needs":
		case "needs you":
		case "close":
		case "behind":
		case "check":
		case "maybe":
			return ClickyInkStatusState.Watch;
		case "bad":
		case "red":
		case "down":
		case "fail":
		case "failing":
		case "failed":
		case "error":
		case "late":
		case "overdue":
		case "broken":
		case "blocked":
		case "critical":
		case "alert":
		case "no":
		case "lost":
		case "missed":
		case "stopped":
		case "off track":
		case "urgent":
			return ClickyInkStatusState.Bad;
		default:
			return ClickyInkStatusState.Off;
		}
	}

	public static string Word(string? written, ClickyInkStatusState state)
	{
		string text = written?.TrimWs();
		if (!string.IsNullOrEmpty(text) && text.CharacterCount() <= 9)
		{
			return text.ToUpperInvariant();
		}
		return state switch
		{
			ClickyInkStatusState.Ok => "OK", 
			ClickyInkStatusState.Watch => "WATCH", 
			ClickyInkStatusState.Bad => "DOWN", 
			_ => "—", 
		};
	}
}
