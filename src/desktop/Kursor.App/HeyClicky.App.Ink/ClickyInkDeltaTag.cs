using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkDeltaTag
{
	public static FrameworkElement? Create(ClickyInkDocument.Block block, double clock, string? given, (string Text, double Value)? computed = null, double size = 11.5, string suffix = "", bool shrinksToFit = false)
	{
		double opacity = ClickyInkChange.Progress(block, clock, 0.5, 0.4);
		string text = ClickyInkDashboardViewMath.DeltaWordsWithoutAChange(given);
		if (text != null)
		{
			InkTextView inkTextView = InkTextView.Plain(text + suffix, ClickyPaperFonts.Rounded(size, FontWeights.Medium), ClickyInkDashboardPalette.Level).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
				t.MinimumScale = (shrinksToFit ? 0.8 : 1.0);
			});
			FrameworkElement obj = (shrinksToFit ? inkTextView : InkDashboardLayout.Fixed(inkTextView));
			obj.Opacity = opacity;
			return obj;
		}
		(string, double)? tuple = ClickyInkDashboardViewMath.ResolvedDelta(given, computed);
		if (tuple.HasValue)
		{
			(string, double) valueOrDefault = tuple.GetValueOrDefault();
			PaperFont font = ClickyPaperFonts.Rounded(size, FontWeights.SemiBold);
			Color color = ClickyInkDashboardPalette.DeltaColor(valueOrDefault.Item2, block);
			FrameworkElement frameworkElement = InkDashboardLayout.Fixed(InkTextView.Plain(ClickyInkDashboardViewMath.DeltaArrow(valueOrDefault.Item2), font, color));
			InkTextView inkTextView2 = InkTextView.Plain(ClickyInkDashboardViewMath.DeltaWords(valueOrDefault.Item1) + suffix, font, color).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
				t.MinimumScale = (shrinksToFit ? 0.8 : 1.0);
			});
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(2.0, InkVerticalAlignment.FirstBaseline, frameworkElement, shrinksToFit ? inkTextView2 : InkDashboardLayout.Fixed(inkTextView2));
			FrameworkElement obj2 = (shrinksToFit ? clickyInkHStack : InkDashboardLayout.Fixed(clickyInkHStack));
			obj2.Opacity = opacity;
			return obj2;
		}
		return null;
	}
}
