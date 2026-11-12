using System;
using System.Collections.Generic;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Ink;

public static class ClickyInkStreakBoxes
{
	public static FrameworkElement Create(IReadOnlyList<ClickyInkStreakDay> days, int? previousCount = null, double progress = 1.0, double appear = 1.0, double box = 14.0, int maximumPerRow = 14, ClickyInkStyle? style = null, int seed = 1)
	{
		ClickyInkStyle style2 = style ?? new ClickyInkStyle(ClickyInkDashboardPalette.Pencil, ClickyInkDashboardPalette.Pencil.Opacity(0.58), null, null);
		int num = Math.Min(maximumPerRow, Math.Max(1, days.Count));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		for (int i = 0; i < days.Count; i += num)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 3.0
			};
			int num2 = i / num;
			for (int j = 0; j < num && i + j < days.Count; j++)
			{
				stackPanel2.Children.Add(Cell(days[i + j], num2 * num + j, days.Count, previousCount, progress, appear, box, style2, seed));
			}
			stackPanel.Children.Add(stackPanel2);
		}
		return stackPanel;
	}

	private static FrameworkElement Cell(ClickyInkStreakDay day, int index, int dayCount, int? previousCount, double progress, double appear, double box, ClickyInkStyle style, int seed)
	{
		int num;
		if (previousCount.HasValue)
		{
			int valueOrDefault = previousCount.GetValueOrDefault();
			num = ((index >= valueOrDefault) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		bool flag2 = (double)index / (double)Math.Max(1, dayCount) <= appear;
		double progress2 = (flag ? progress : 1.0);
		Grid grid = new Grid
		{
			Width = box,
			Height = box,
			IsHitTestVisible = false
		};
		switch (day)
		{
		case ClickyInkStreakDay.Done:
		{
			FrameworkElement frameworkElement = ClickyInkHatch.Create(flag ? style.Fresh : ClickyPaperInk.DoneGreen, progress2, 2.0, 1.2, seed + index, 0.0, Math.Max(0.0, box - 3.0), Math.Max(0.0, box - 3.0), null);
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(frameworkElement);
			break;
		}
		case ClickyInkStreakDay.Missed:
			grid.Children.Add(new Ellipse
			{
				Width = 2.5,
				Height = 2.5,
				Fill = Ui.Brush(style.Quiet.Opacity(0.45))
			});
			break;
		case ClickyInkStreakDay.Rest:
			grid.Children.Add(new PaperDrawing().Stroke(new PaperPath().MoveTo(3.0, box - 3.0).LineTo(box - 3.0, 3.0), new PaperStroke(style.Quiet.Opacity(0.7))));
			break;
		}
		grid.Children.Add(ClickyShapeView.Stroke(new ClickyInkBox(index + seed), ClickyInkDashboardPalette.Pencil.Opacity((day == ClickyInkStreakDay.Upcoming) ? 0.25 : 0.6), 1.0));
		if (day == ClickyInkStreakDay.Today)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(index), style.Fresh, 1.4);
			clickyShapeView.Margin = new Thickness(-3.0);
			grid.Children.Add(clickyShapeView);
		}
		grid.Opacity = (flag2 ? 1 : 0);
		return grid;
	}
}
