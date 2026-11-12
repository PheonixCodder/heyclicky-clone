using HeyClicky.Core.Ink;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkWordsView
{
	public static InkTextView Make(ClickyInkLiveNote.Words words, double clock, ClickyInkStyle style, PaperFont font, bool pencil = true, Color? color = null, InkRange? struckOver = null)
	{
		return new InkTextView(ClickyInkWords.Text(words, clock, style, pencil, color, struckOver), font, style.Ink);
	}
}
