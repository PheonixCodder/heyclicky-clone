namespace HeyClicky.App.Ink;

public abstract record ClickyInkNoteTarget
{
	public sealed record Clicky(string Slug) : ClickyInkNoteTarget();

	public sealed record Job(string Id) : ClickyInkNoteTarget();
}
