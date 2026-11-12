namespace HeyClicky.App.Ink;

public sealed record ClickyInkCardSubject(string Id, SubjectKind Kind, bool IsRunning, SubjectPhase Phase, ClickyThingStatus Status, ClickyPaperStock Stock)
{
	public enum SubjectKind
	{
		Job,
		Kept
	}

	public enum SubjectPhase
	{
		Working,
		NeedsYou,
		Failed,
		Done
	}
}
