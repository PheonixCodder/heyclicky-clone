using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackInkStates
{
	public static ClickyThingStatus Status(this ClickyEdgeStackInkReading.State state)
	{
		return state switch
		{
			ClickyEdgeStackInkReading.State.Writing => ClickyThingStatus.Working, 
			ClickyEdgeStackInkReading.State.NeedsYou => ClickyThingStatus.NeedsYou, 
			ClickyEdgeStackInkReading.State.Ready => ClickyThingStatus.Ready, 
			ClickyEdgeStackInkReading.State.Done => ClickyThingStatus.Done, 
			ClickyEdgeStackInkReading.State.Watching => ClickyThingStatus.Watching, 
			_ => ClickyThingStatus.Paused, 
		};
	}

	public static bool WantsAttention(this ClickyEdgeStackInkReading.State state)
	{
		if ((uint)state <= 2u)
		{
			return true;
		}
		return false;
	}
}
