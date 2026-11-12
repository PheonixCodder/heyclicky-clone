using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkProgressBar
{
	public static FrameworkElement Create(double fraction, double? previous = null, double progress = 1.0, double appear = 1.0, double height = 14.0, ClickyInkStyle? style = null, int seed = 1, double? width = null)
	{
		ClickyInkStyle inkStyle = style ?? new ClickyInkStyle(ClickyInkDashboardPalette.Pencil, ClickyInkDashboardPalette.Pencil.Opacity(0.58), null, null);
		return new ClickyInkDashboardCanvas(delegate(PaperDrawing drawing, Size size)
		{
			Draw(drawing, size.Width, height, fraction, previous, progress, appear, inkStyle, seed);
		}, width, height, clips: false);
	}

	private static void Draw(PaperDrawing drawing, double width, double height, double fraction, double? previous, double progress, double appear, ClickyInkStyle style, int seed)
	{
		double num = Math.Min(fraction, previous ?? fraction);
		double num2;
		if (previous.HasValue)
		{
			double valueOrDefault = previous.GetValueOrDefault();
			num2 = valueOrDefault + (fraction - valueOrDefault) * progress;
		}
		else
		{
			num2 = fraction;
		}
		double num3 = num2;
		double value = Math.Max(0.0, width * ((!previous.HasValue) ? num3 : num));
		Place(drawing, ClickyInkHatch.Create(ClickyInkDashboardPalette.Pencil.Opacity(0.85), appear, 2.4, 1.2, seed, 0.0, value, height, null), 0.0, 0.0);
		if (previous.HasValue)
		{
			double valueOrDefault2 = previous.GetValueOrDefault();
			if (fraction > valueOrDefault2)
			{
				Place(drawing, ClickyInkHatch.Create(style.Fresh, 1.0, 2.4, 1.3, seed + 9, 0.0, Math.Max(0.0, width * (num3 - valueOrDefault2)), height, null), width * valueOrDefault2, 0.0);
			}
		}
		for (int i = 1; i < 4; i++)
		{
			drawing.Fill(new PaperPath().AddRect(new Rect(width * (double)i / 4.0, height + 2.0, 1.0, 4.0)), ClickyInkDashboardPalette.Pencil.Opacity(0.35));
		}
		if (previous.HasValue)
		{
			double valueOrDefault3 = previous.GetValueOrDefault();
			if (valueOrDefault3 != fraction && progress > 0.0)
			{
				drawing.Stroke(new PaperPath().MoveTo(width * valueOrDefault3, -4.0).LineTo(width * valueOrDefault3 + 1.0, height + 3.0), PaperStroke.Round(style.Quiet, 1.2));
			}
		}
		drawing.Stroke(new ClickyInkRuledBox(seed, 3.0).PathIn(new Rect(0.0, 0.0, width, height)).Trimmed(0.0, appear), PaperStroke.Round(ClickyInkDashboardPalette.Pencil.Opacity(0.7), 1.1));
	}

	private static void Place(PaperDrawing drawing, FrameworkElement element, double x, double y)
	{
		Canvas.SetLeft(element, x);
		Canvas.SetTop(element, y);
		drawing.Children.Add(element);
	}
}
