using HeyClicky.Core.Ink;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkLine
{
	public static InkTextView Make(ClickyInkLiveNote.Words words, double clock, ClickyInkStyle style, PaperFont font, Color? color = null, double? goneAt = null, int? lineLimit = null, bool fillsWidth = true)
	{
		InkRange? struckOver;
		if (goneAt.HasValue)
		{
			double valueOrDefault = goneAt.GetValueOrDefault();
			struckOver = new InkRange(valueOrDefault, valueOrDefault + 0.3);
		}
		else
		{
			struckOver = null;
		}
		return new InkTextView(ClickyInkWords.Text(words, clock, style, pencil: true, color, struckOver), font, style.Ink)
		{
			LineLimit = lineLimit,
			FillsWidth = fillsWidth
		};
	}
}
