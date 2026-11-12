namespace HeyClicky.App.Ink;

public sealed record ClickyNoteFace(string Title, ClickyPaperGlyph Glyph, NoteBody Body)
{
	public abstract record NoteBody
	{
		public sealed record Lines(NoteLine[] Items) : NoteBody();

		public sealed record Chart(double[] Series, bool ShowsTrend) : NoteBody();
	}

	public sealed record NoteLine(string Text)
	{
		public LineMark Mark { get; init; } = new LineMark.None();

		public LineTone Tone { get; init; }

		public string? Value { get; init; }

		public bool IsCircled { get; init; }

		public LineRun[]? Runs { get; init; }

		public bool HasCaret { get; init; }
	}

	public abstract record LineMark
	{
		public sealed record None : LineMark;

		public sealed record Done : LineMark;

		public sealed record Now : LineMark;

		public sealed record Todo : LineMark;

		public sealed record Number(int Value) : LineMark();
	}

	public enum LineTone
	{
		Ink,
		Clicky,
		Quiet
	}

	public sealed record LineRun(string Text, bool IsClicky);

	public abstract record NoteFooter
	{
		public sealed record None : NoteFooter;

		public sealed record Actions(NoteAction[] Items) : NoteFooter();

		public sealed record Files(string[] Names) : NoteFooter();

		public sealed record Progress(int Step, int Total, double Fraction) : NoteFooter();

		public sealed record Text(string Words) : NoteFooter();
	}

	public sealed record NoteAction(string Id, string Title, ClickyPaperGel Gel = ClickyPaperGel.Blue);

	public string? Meta { get; init; }

	public NoteFooter Footer { get; init; } = new NoteFooter.None();

	public ClickyThingStatus? Stamp { get; init; }

	public bool IsIdea { get; init; }

	public ClickyPaperGlyph? MetaLogo { get; init; }

	public bool MetaIsClicky { get; init; }
}
