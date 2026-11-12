using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackInkReadings
{
	public static ClickyEdgeStackInkReading Make(ClickyInkLiveNote note, double clock, ClickyInkCardSubject thing, double? seenClock, ClickyInkActionFeedback feedback)
	{
		bool isRunning = thing.IsRunning;
		ClickyInkCardSubject.SubjectPhase phase = thing.Phase;
		bool flag = (uint)(phase - 1) <= 1u;
		bool thingWantsTheUser = flag;
		return new ClickyEdgeStackInkReading(note, clock, isRunning, thingWantsTheUser, thing.Status switch
		{
			ClickyThingStatus.Ready => ClickyEdgeStackInkReading.State.Ready, 
			ClickyThingStatus.Watching => ClickyEdgeStackInkReading.State.Watching, 
			ClickyThingStatus.Paused => ClickyEdgeStackInkReading.State.Paused, 
			_ => ClickyEdgeStackInkReading.State.Done, 
		}, seenClock, feedback);
	}
}
