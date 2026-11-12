using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyCardSparkline : Grid
{
	public ClickyCardSparkline(IReadOnlyList<double> values, Color ink, Color ringColor, bool circledEnd = false)
	{
		base.IsHitTestVisible = false;
		PaperDrawing drawing = new PaperDrawing();
		base.Children.Add(drawing);
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			drawing.Children.Clear();
			Size size = e.NewSize;
			if (values.Count > 1)
			{
				double low = values.Min();
				double num = values.Max();
				double span = Math.Max(num - low, 0.0001);
				List<Point> list = values.Select((double value, int index) => new Point(4.0 + (double)index / (double)(values.Count - 1) * (size.Width - 8.0 - 8.0), 4.0 + (1.0 - (value - low) / span) * (size.Height - 8.0 - 6.0))).ToList();
				PaperPath paperPath = new PaperPath().MoveTo(list[0]);
				for (int i = 1; i < list.Count; i++)
				{
					Point control = list[i - 1];
					Point p = list[i];
					paperPath.QuadTo(new Point((control.X + p.X) / 2.0, (control.Y + p.Y) / 2.0), control);
					if (i == list.Count - 1)
					{
						paperPath.LineTo(p);
					}
				}
				drawing.Stroke(paperPath, new PaperStroke(ink, 2.0, PenLineCap.Round, PenLineJoin.Round));
				Point point = list[list.Count - 1];
				drawing.Fill(new PaperPath().AddEllipse(new Rect(point.X - 3.2, point.Y - 3.2, 6.4, 6.4)), ink);
				if (circledEnd)
				{
					drawing.Stroke(new ClickyHandRing(5, 0.18).PathIn(new Rect(point.X - 12.0, point.Y - 11.0, 24.0, 22.0)), new PaperStroke(ringColor, 2.0, PenLineCap.Round));
				}
			}
		};
	}
}
