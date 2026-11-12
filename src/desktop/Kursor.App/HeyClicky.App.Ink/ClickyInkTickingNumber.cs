using System;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTickingNumber
{
	public static FrameworkElement Create(ClickyInkDocument.Block block, string key, double clock, ClickyInkStyle style, double size = 30.0, double delay = 0.0, string? value = null, string? previous = null, bool showsWas = true)
	{
		string text = value ?? block[key] ?? "";
		string text2 = previous ?? ClickyInkChange.Previous(block, key);
		double num = ClickyInkChange.Progress(block, clock, delay);
		bool num2 = text2 != null && text2 != text && num < 1.0;
		int num4;
		if (text2 != null && text2 != text)
		{
			int? num3 = ClickyInkChange.ChangedEdit(block);
			if (num3.HasValue)
			{
				num4 = ((num3.GetValueOrDefault() == style.LatestEdit) ? 1 : 0);
				goto IL_00a9;
			}
		}
		num4 = 0;
		goto IL_00a9;
		IL_00a9:
		bool flag = (byte)num4 != 0;
		string text3 = (num2 ? (ClickyInkNumber.Interpolated(text2, text, num) ?? ((num < 0.35) ? (text2 ?? text) : text)) : text);
		double valueOrDefault = ClickyInkChange.ChangedAt(block).GetValueOrDefault();
		double highlight = (flag ? ClickyInkClock.Progress(clock, valueOrDefault + delay + 0.7, valueOrDefault + delay + 1.1) : 0.0);
		Color color = ((num2 || flag) ? style.Fresh : ((text3.Length == 0) ? style.Quiet : style.Ink));
		InkTextView inkTextView = InkTextView.Plain((text3.Length == 0) ? "–" : text3, ClickyPaperFonts.BigNumber(size), color).With(delegate(InkTextView t)
		{
			t.Tracking = -0.8;
			t.MonospacedDigit = true;
			t.LineLimit = 1;
			t.MinimumScale = 0.45;
		});
		FrameworkElement frameworkElement = inkTextView;
		if (highlight > 0.0)
		{
			ClickyShapeView background = new ClickyShapeView(new ClickyShape((Rect r) => InkDashboardLayout.RotatedRoundedRect(new Rect(r.X - 2.0, r.Y + r.Height * 0.3, r.Width * highlight + 4.0, r.Height * 0.62), 3.0, -1.0)), Ui.Brush(style.Highlighter));
			frameworkElement = new ClickyInkOverlay(inkTextView).Behind(background);
		}
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(5.0, InkVerticalAlignment.FirstBaseline, frameworkElement);
		if (showsWas && flag && text2 != null && text2.Length > 0)
		{
			bool single = text2.CharacterCount() == 1;
			PaperFont font = (single ? ClickyPaperFonts.Hand(Math.Max(12.0, size * 0.5)) : ClickyPaperFonts.BigNumber(Math.Max(11.0, size * 0.42)));
			InkTextView inkTextView2 = InkTextView.Plain(single ? ("from " + text2) : text2, font, style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
				if (!single)
				{
					t.Strikethrough = style.Fresh;
				}
			});
			inkTextView2.Opacity = ClickyInkClock.Progress(clock, valueOrDefault + delay, valueOrDefault + delay + 0.3);
			clickyInkHStack.Children.Add(InkDashboardLayout.Fixed(inkTextView2));
		}
		return clickyInkHStack;
	}
}
