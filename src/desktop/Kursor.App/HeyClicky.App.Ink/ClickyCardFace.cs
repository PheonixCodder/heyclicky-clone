namespace HeyClicky.App.Ink;

public sealed record ClickyCardFace(string Title, ClickyPaperGlyph Glyph, ClickyPaperStock Stock)
{
	public abstract record CardHeadline
	{
		public sealed record Big(string Text) : CardHeadline();

		public sealed record Count(string Number, string Word) : CardHeadline();

		public sealed record Sentence(string Text) : CardHeadline();

		public sealed record Message(string From, string Text, string Draft) : CardHeadline();
	}

	public abstract record CardDetail
	{
		public sealed record Sparkline(double[] Values, bool CircledEnd) : CardDetail();

		public sealed record Rows(CardRow[] Items) : CardDetail();

		public sealed record Bullets(string[] Items) : CardDetail();

		public sealed record Waiting(string[] Names) : CardDetail();
	}

	public sealed record CardRow(string Label, string Value);

	public enum CardFootTone
	{
		Deep,
		Good,
		Quiet
	}

	public string? Caps { get; init; }

	public CardHeadline? Headline { get; init; }

	public CardDetail? Detail { get; init; }

	public string? Foot { get; init; }

	public CardFootTone FootTone { get; init; }

	public ClickyPaperStickerKind? Sticker { get; init; }
}
