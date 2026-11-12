namespace HeyClicky.App.Ink;

public readonly record struct ClickyHandTextMetrics(double Pitch, double HalfLeading)
{
	public static ClickyHandTextMetrics For(PaperFont font, double size, double lineHeight)
	{
		double num = size * lineHeight;
		return new ClickyHandTextMetrics(num, (num - ClickyHandLineBreaker.NaturalLineHeight(font)) / 2.0);
	}
}
