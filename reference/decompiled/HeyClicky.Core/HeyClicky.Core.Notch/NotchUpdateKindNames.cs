namespace HeyClicky.Core.Notch;

public static class NotchUpdateKindNames
{
	public static string RawValue(this NotchUpdateKind kind)
	{
		return kind switch
		{
			NotchUpdateKind.NeedsYou => "needsYou", 
			NotchUpdateKind.Failed => "failed", 
			_ => "done", 
		};
	}

	public static NotchUpdateKind Parse(string raw)
	{
		if (!(raw == "needsYou"))
		{
			if (raw == "failed")
			{
				return NotchUpdateKind.Failed;
			}
			return NotchUpdateKind.Done;
		}
		return NotchUpdateKind.NeedsYou;
	}

	public static string AnalyticsName(this NotchUpdateKind kind)
	{
		return kind switch
		{
			NotchUpdateKind.NeedsYou => "needs_you", 
			NotchUpdateKind.Failed => "failed", 
			_ => "done", 
		};
	}
}
