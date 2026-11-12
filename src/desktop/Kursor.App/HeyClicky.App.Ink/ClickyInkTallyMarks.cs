using System;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkTallyMarks
{
	public const double GroupWidth = 26.0;

	public const double GroupGap = 9.0;

	public static int Capacity(double width, int rows)
	{
		return ClickyInkDashboardViewMath.TallyCapacity(width, rows);
	}

	public static FrameworkElement Create(int count, int fresh = 0, double progress = 1.0, double appear = 1.0, double markHeight = 20.0, ClickyInkStyle? style = null, int seed = 1, double? width = null, double? height = null)
	{
		ClickyInkStyle inkStyle = style ?? new ClickyInkStyle(ClickyInkDashboardPalette.Pencil, ClickyInkDashboardPalette.Pencil.Opacity(0.58), null, null);
		return new ClickyInkDashboardCanvas(delegate(PaperDrawing drawing, Size size)
		{
			Draw(drawing, size, count, fresh, progress, appear, markHeight, inkStyle, seed);
		}, width, height);
	}

	private static void Draw(PaperDrawing drawing, Size size, int count, int fresh, double progress, double appear, double markHeight, ClickyInkStyle style, int seed)
	{
		int num = ClickyInkDashboardViewMath.TallyGroupsPerRow(size.Width);
		double num2 = markHeight + 8.0;
		int num3 = num * 5 * Math.Max(1, (int)((size.Height + 8.0) / num2));
		int num4 = Math.Min(count, num3);
		int num5 = num4 - Math.Min(fresh, num4);
		double num6 = (double)Math.Min(fresh, num4) * progress;
		for (int i = 0; i < num4; i++)
		{
			int num7 = i / 5;
			int num8 = i % 5;
			int num9 = num7 / num;
			double num10 = (double)(num7 % num) * 35.0;
			double num11 = (double)num9 * num2;
			ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed * 131 + i * 7 + 3));
			PaperPath paperPath = new PaperPath();
			if (num8 < 4)
			{
				double num12 = num10 + 3.0 + (double)num8 * 5.5;
				double x = num12 + J();
				double y = num11 + 1.0 + J(0.6);
				paperPath.MoveTo(x, y);
				double x2 = num12 + 1.2 + J();
				double y2 = num11 + markHeight + J(0.6);
				double x3 = num12 + J(1.2);
				paperPath.QuadTo(new Point(x2, y2), new Point(x3, num11 + markHeight / 2.0));
			}
			else
			{
				double x4 = num10 - 1.0 + J();
				double y3 = num11 + markHeight * 0.78 + J();
				paperPath.MoveTo(x4, y3);
				double x5 = num10 + 26.0 + 1.0 + J();
				double y4 = num11 + markHeight * 0.18 + J();
				paperPath.LineTo(x5, y4);
			}
			bool flag = i >= num5 && fresh > 0;
			double num13 = (flag ? Math.Min(1.0, num6 - (double)(i - num5)) : Math.Min(1.0, appear * (double)num4 / 1.5 - (double)i / 1.5 + 1.0));
			if (!(num13 <= 0.0))
			{
				drawing.Stroke((num13 < 1.0) ? paperPath.Trimmed(0.0, num13) : paperPath, new PaperStroke(flag ? style.Fresh : ClickyInkDashboardPalette.Pencil.Opacity(0.85), 1.7, PenLineCap.Round));
			}
			double J(double amount = 0.9)
			{
				return generator.Next(0.0 - amount, amount);
			}
		}
		if (count > num3)
		{
			TextBlock text = ClickyPaperFonts.Rounded(11.0, FontWeights.SemiBold).Apply(new TextBlock
			{
				Text = $"+{count - num3}",
				Foreground = Ui.Brush(style.Quiet),
				IsTextScaleFactorEnabled = false
			});
			ClickyInkDashboardCanvas.DrawText(drawing, text, new Point(size.Width - 14.0, size.Height - 8.0));
		}
	}
}
