using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Ink;

public static class ClickyInkWasValue
{
	public static FrameworkElement Make(string value, ClickyInkStyle style)
	{
		InkTextView inkTextView = InkTextView.Plain(value, ClickyPaperFonts.BigNumber(17.0), style.Quiet).With(delegate(InkTextView t)
		{
			t.Strikethrough = style.Fresh;
			t.LineLimit = 1;
		});
		inkTextView.Transformed(0.0, 0.0, -6.0, 1.0, null);
		inkTextView.Margin = new Thickness(0.0, 3.0, 0.0, 3.0);
		inkTextView.HorizontalAlignment = HorizontalAlignment.Left;
		return inkTextView;
	}
}
