namespace HeyClicky.App.Ink;

public sealed record ClickyInkCardSubject(string Id, ClickyInkCardSubject.SubjectKind Kind, bool IsRunning, ClickyInkCardSubject.SubjectPhase Phase, ClickyThingStatus Status, ClickyPaperStock Stock)
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
