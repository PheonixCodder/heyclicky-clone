using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkSparkLine
{
	public static FrameworkElement Create(IReadOnlyList<double> values, IReadOnlyList<double>? previous = null, double progress = 1.0, double appear = 1.0, double? goal = null, ClickyInkStyle? style = null, Color? color = null, int seed = 1, bool marksNew = true, double? width = null, double? height = null)
	{
		ClickyInkStyle inkStyle = style ?? new ClickyInkStyle(ClickyInkDashboardPalette.Pencil, ClickyInkDashboardPalette.Pencil.Opacity(0.58), null, null);
		return new ClickyInkDashboardCanvas(delegate(PaperDrawing drawing, Size size)
		{
			Draw(drawing, size, values, previous, progress, appear, goal, inkStyle, color, seed, marksNew);
		}, width, height);
	}

	private static void Draw(PaperDrawing drawing, Size size, IReadOnlyList<double> values, IReadOnlyList<double>? previous, double progress, double appear, double? goal, ClickyInkStyle style, Color? color, int seed, bool marksNew)
	{
		if (values.Count == 0)
		{
			drawing.Stroke(new PaperPath().MoveTo(0.0, size.Height * 0.7).LineTo(size.Width, size.Height * 0.7), new PaperStroke(style.Quiet.Opacity(0.5), 1.0, PenLineCap.Flat, PenLineJoin.Miter, new double[2] { 2.0, 4.0 }));
			return;
		}
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 13));
		List<double> source;
		if (goal.HasValue)
		{
			double valueOrDefault = goal.GetValueOrDefault();
			source = values.Append(valueOrDefault).ToList();
		}
		else
		{
			source = values.ToList();
		}
		double num = source.Min();
		double num2 = source.Max() - num;
		double num3 = size.Width - 3.0 - 7.0;
		double num4 = size.Height - 5.0 - 5.0;
		List<Point> list = new List<Point>(values.Count);
		for (int i = 0; i < values.Count; i++)
		{
			double num5 = ((values.Count == 1) ? (3.0 + num3) : (3.0 + num3 * (double)i / (double)(values.Count - 1)));
			double num6 = ((num2 == 0.0) ? (5.0 + num4 / 2.0) : (5.0 + num4 * (1.0 - (values[i] - num) / num2)));
			double num7 = clickyPaperRandom.Next(-0.4, 0.4);
			double num8 = clickyPaperRandom.Next(-0.4, 0.4);
			list.Add(new Point(num5 + num7, num6 + num8));
		}
		ClickyInkSeries.Change? change = ((previous == null) ? ((ClickyInkSeries.Change?)null) : ClickyInkSeries.ChangeBetween(previous, values));
		int num9 = ((progress < 1.0) ? (change?.Added ?? 0) : 0);
		double num10 = Math.Min((double)Math.Max(0, list.Count - 1 - num9) + (double)num9 * progress, (double)(list.Count - 1) * appear);
		if (goal.HasValue)
		{
			double valueOrDefault2 = goal.GetValueOrDefault();
			if (num2 > 0.0)
			{
				double y = 5.0 + num4 * (1.0 - (valueOrDefault2 - num) / num2);
				drawing.Stroke(new PaperPath().MoveTo(0.0, y).LineTo(size.Width, y), new PaperStroke(style.Quiet.Opacity(0.55), 1.0, PenLineCap.Flat, PenLineJoin.Miter, new double[2] { 3.0, 3.0 }));
			}
		}
		Color color2 = color ?? ClickyInkDashboardPalette.Pencil;
		if (list.Count == 1)
		{
			drawing.Fill(new PaperPath().AddEllipse(new Rect(list[0].X - 3.0, list[0].Y - 3.0, 6.0, 6.0)), color2);
			return;
		}
		for (int j = 0; j < list.Count - 1; j++)
		{
			double num11 = Math.Min(1.0, num10 - (double)j);
			if (num11 <= 0.0)
			{
				break;
			}
			Point p = list[j];
			Point point = list[j + 1];
			Point p2 = new Point(p.X + (point.X - p.X) * num11, p.Y + (point.Y - p.Y) * num11);
			bool flag = marksNew && change.HasValue && j >= list.Count - 1 - change.Value.Added;
			drawing.Stroke(new PaperPath().MoveTo(p).LineTo(p2), PaperStroke.Round(flag ? style.Fresh : color2.Opacity(0.85), 1.6));
		}
		bool flag2 = change.HasValue && marksNew;
		for (int k = 0; k < list.Count; k++)
		{
			if (!((double)k > num10 + 0.001))
			{
				Point point2 = list[k];
				bool flag3 = k == list.Count - 1;
				double num12 = (flag3 ? 2.8 : 1.4);
				drawing.Fill(new PaperPath().AddEllipse(new Rect(point2.X - num12, point2.Y - num12, num12 * 2.0, num12 * 2.0)), (!flag3) ? color2.Opacity(0.55) : (flag2 ? style.Fresh : color2));
				if (flag3)
				{
					Rect rect = new Rect(point2.X - 6.0, point2.Y - 6.0, 12.0, 12.0);
					drawing.Stroke(new ClickyHandRing(seed).PathIn(rect), new PaperStroke((flag2 ? style.Fresh : color2).Opacity(0.7), 1.1, PenLineCap.Round, PenLineJoin.Miter, new double[2] { 2.2, 1.8 }));
				}
			}
		}
	}
}
