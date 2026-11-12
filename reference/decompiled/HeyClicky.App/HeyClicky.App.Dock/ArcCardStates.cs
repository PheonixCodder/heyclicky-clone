using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public static class ArcCardStates
{
	public const uint InkRgb = 1842724u;

	public const uint CoralRgb = 14242877u;

	public const uint WorkingRgb = 1755979u;

	public const uint BlueRgb = 3112182u;

	public static ArcCardState FromJobPhase(ClickyJobRailPhase phase, bool isSeen)
	{
		return phase switch
		{
			ClickyJobRailPhase.Queued => ArcCardState.Queued, 
			ClickyJobRailPhase.Working => ArcCardState.Working, 
			ClickyJobRailPhase.NeedsYou => ArcCardState.NeedsYou, 
			ClickyJobRailPhase.Failed => ArcCardState.Failed, 
			ClickyJobRailPhase.Stopped => ArcCardState.Stopped, 
			_ => isSeen ? ArcCardState.Done : ArcCardState.New, 
		};
	}

	public static ArcCardState FromLatest(ArcLatestKind? kind, bool isUnread)
	{
		return kind switch
		{
			ArcLatestKind.Working => ArcCardState.Working, 
			ArcLatestKind.NeedsYou => ArcCardState.NeedsYou, 
			ArcLatestKind.Failed => ArcCardState.Failed, 
			ArcLatestKind.Finished => isUnread ? ArcCardState.New : ArcCardState.Done, 
			_ => ArcCardState.Idle, 
		};
	}

	public static string Title(ArcCardState state)
	{
		return state switch
		{
			ArcCardState.Queued => "Queued", 
			ArcCardState.Working => "Working", 
			ArcCardState.NeedsYou => "Needs you", 
			ArcCardState.Failed => "Failed", 
			ArcCardState.Stopped => "Interrupted", 
			ArcCardState.New => "New", 
			ArcCardState.Done => "Done", 
			ArcCardState.Idle => "Idle", 
			ArcCardState.Pinning => "Pinning", 
			_ => "Ready to start", 
		};
	}

	public static (uint Rgb, double Opacity) Color(ArcCardState state)
	{
		switch (state)
		{
		case ArcCardState.Queued:
		case ArcCardState.Working:
			return (Rgb: 1755979u, Opacity: 1.0);
		case ArcCardState.New:
		case ArcCardState.Pinning:
		case ArcCardState.Suggested:
			return (Rgb: 3112182u, Opacity: 1.0);
		case ArcCardState.NeedsYou:
		case ArcCardState.Failed:
			return (Rgb: 14242877u, Opacity: 1.0);
		default:
			return (Rgb: 1842724u, Opacity: 0.5);
		}
	}

	public static bool IsBusy(ArcCardState state)
	{
		if ((uint)state <= 1u || state == ArcCardState.Pinning)
		{
			return true;
		}
		return false;
	}

	public static bool IsWorking(ArcCardState state)
	{
		if ((uint)state <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool IsDone(ArcCardState state)
	{
		if ((uint)(state - 5) <= 1u)
		{
			return true;
		}
		return false;
	}
}
