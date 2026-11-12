using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickySketchPath
{
	public static List<Point> RoundedRectPoints(Rect rect, double radius, double step = 7.0)
	{
		double r = Math.Min(radius, Math.Min(rect.Width / 2.0, rect.Height / 2.0));
		List<Point> points = new List<Point>();
		double x = rect.X;
		double y = rect.Y;
		double width = rect.Width;
		double height = rect.Height;
		Edge(new Point(x + r, y), new Point(x + width - r, y));
		Arc(new Point(x + width - r, y + r), -Math.PI / 2.0, 0.0);
		Edge(new Point(x + width, y + r), new Point(x + width, y + height - r));
		Arc(new Point(x + width - r, y + height - r), 0.0, Math.PI / 2.0);
		Edge(new Point(x + width - r, y + height), new Point(x + r, y + height));
		Arc(new Point(x + r, y + height - r), Math.PI / 2.0, Math.PI);
		Edge(new Point(x, y + height - r), new Point(x, y + r));
		Arc(new Point(x + r, y + r), Math.PI, 4.71238898038469);
		return points;
		void Arc(Point center, double start, double end)
		{
			int num3 = Math.Max(2, (int)(Math.Abs(end - start) * r / step) + 1);
			for (int j = 0; j < num3; j++)
			{
				double num4 = start + (end - start) * (double)j / (double)num3;
				points.Add(new Point(center.X + r * Math.Cos(num4), center.Y + r * Math.Sin(num4)));
			}
		}
		void Edge(Point from, Point to)
		{
			int num = Math.Max(2, (int)(Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y)) / step));
			for (int i = 0; i < num; i++)
			{
				double num2 = (double)i / (double)num;
				points.Add(new Point(from.X + (to.X - from.X) * num2, from.Y + (to.Y - from.Y) * num2));
			}
		}
	}

	public static PaperPath Smooth(IReadOnlyList<Point> input, bool closed)
	{
		List<Point> list = input.ToList();
		if (list.Count < 2)
		{
			return new PaperPath();
		}
		if (closed)
		{
			list.AddRange(list.Take(2).ToList());
		}
		PaperPath paperPath = new PaperPath();
		paperPath.MoveTo(new Point((list[0].X + list[1].X) / 2.0, (list[0].Y + list[1].Y) / 2.0));
		for (int i = 1; i < list.Count - 1; i++)
		{
			Point to = new Point((list[i].X + list[i + 1].X) / 2.0, (list[i].Y + list[i + 1].Y) / 2.0);
			paperPath.QuadTo(to, list[i]);
		}
		if (closed)
		{
			paperPath.Close();
		}
		return paperPath;
	}

	public static PaperPath RoundedRect(Rect rect, double radius, long seed, double jitter = 0.7)
	{
		ClickySketchRandom random = new ClickySketchRandom(seed);
		double drift = 0.0;
		return Smooth(RoundedRectPoints(rect, radius).Select(delegate(Point point)
		{
			drift = drift * 0.6 + random.Uniform(0.0 - jitter, jitter) * 0.4;
			double x = point.X + drift + random.Uniform(-0.25, 0.25);
			double y = point.Y + drift * 0.8 + random.Uniform(-0.25, 0.25);
			return new Point(x, y);
		}).ToList(), closed: true);
	}

	public static PaperPath Line(Point start, Point end, long seed, double jitter = 0.6, double step = 10.0)
	{
		ClickySketchRandom clickySketchRandom = new ClickySketchRandom(seed);
		int num = Math.Max(2, (int)(Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y)) / step));
		List<Point> list = new List<Point>();
		for (int i = 0; i <= num; i++)
		{
			double num2 = (double)i / (double)num;
			double x = start.X + (end.X - start.X) * num2 + clickySketchRandom.Uniform(-0.3, 0.3);
			double y = start.Y + (end.Y - start.Y) * num2 + clickySketchRandom.Uniform(0.0 - jitter, jitter);
			list.Add(new Point(x, y));
		}
		return Smooth(list, closed: false).LineTo(end);
	}
}
