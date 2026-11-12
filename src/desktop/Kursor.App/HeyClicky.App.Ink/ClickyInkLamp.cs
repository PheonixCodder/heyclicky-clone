using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkLamp
{
	public static FrameworkElement Create(ClickyInkStatusState state, ClickyInkStatusState? previous = null, double progress = 1.0, double diameter = 22.0, int seed = 1, double appear = 1.0)
	{
		Grid grid = new Grid
		{
			Width = diameter,
			Height = diameter,
			IsHitTestVisible = false
		};
		double spacing = Math.Max(2.0, diameter / 9.0);
		double lineWidth = Math.Max(1.1, diameter / 15.0);
		int num;
		if (previous.HasValue)
		{
			ClickyInkStatusState valueOrDefault = previous.GetValueOrDefault();
			num = ((valueOrDefault != state) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (flag && progress < 1.0)
		{
			grid.Children.Add(ClickyInkHatch.Create(ClickyInkDashboardPalette.Lamp(previous.Value), 1.0, spacing, lineWidth, seed + 3, 0.0, diameter, diameter, diameter * 0.08));
		}
		grid.Children.Add(ClickyInkHatch.Create(ClickyInkDashboardPalette.Lamp(state), Math.Min(appear, flag ? progress : 1.0), spacing, lineWidth, seed, 0.0, diameter, diameter, diameter * 0.08));
		double num2 = diameter * 0.24;
		double num3 = (diameter - 2.0 * num2) / 2.0;
		PaperPath paperPath = new PaperPath();
		for (int i = 0; i <= 12; i++)
		{
			double num4 = (0.56 + 0.1399999999999999 * (double)i / 12.0) * 2.0 * Math.PI;
			Point p = new Point(diameter / 2.0 + num3 * Math.Cos(num4), diameter / 2.0 + num3 * Math.Sin(num4));
			if (i == 0)
			{
				paperPath.MoveTo(p);
			}
			else
			{
				paperPath.LineTo(p);
			}
		}
		grid.Children.Add(new PaperDrawing().Stroke(paperPath, PaperStroke.Round(PaperColor.Hex("#FFFDF6").Opacity(0.85), Math.Max(1.4, diameter / 11.0))));
		grid.Children.Add(ClickyShapeView.Stroke(new ClickyHandRing(seed, 0.1).Trim(0.0, appear), new PaperStroke(ClickyInkDashboardPalette.Pencil.Opacity(0.75), Math.Max(1.1, diameter / 17.0), PenLineCap.Round)));
		return grid;
	}
}
