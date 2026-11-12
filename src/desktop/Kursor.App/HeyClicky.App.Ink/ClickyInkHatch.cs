using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkHatch
{
	public static FrameworkElement Create(Color color, double progress = 1.0, double spacing = 2.6, double lineWidth = 1.5, int seed = 1, double from = 0.0, double? width = null, double? height = null, double? circleInset = null)
	{
		return new ClickyInkDashboardCanvas(delegate(PaperDrawing drawing, Size size)
		{
			Draw(drawing, size, color, progress, spacing, lineWidth, seed, from, circleInset);
		}, width, height);
	}

	public static void Draw(PaperDrawing drawing, Size size, Color color, double progress = 1.0, double spacing = 2.6, double lineWidth = 1.5, int seed = 1, double from = 0.0, double? circleInset = null, double offsetX = 0.0, double offsetY = 0.0)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 41));
		double num = size.Height * 0.55;
		double num2 = size.Width + num;
		int num3 = Math.Max(1, (int)(num2 / spacing));
		double num4 = (double)num3 * progress;
		Point c = new Point(size.Width / 2.0, size.Height / 2.0);
		double r = Math.Min(size.Width, size.Height) / 2.0 - circleInset.GetValueOrDefault();
		for (int i = 0; i < num3; i++)
		{
			double num5 = (double)i / (double)num3;
			double num6 = clickyPaperRandom.Next(-0.7, 0.7);
			double num7 = clickyPaperRandom.Next(-0.7, 0.7);
			double opacity = clickyPaperRandom.Next(0.62, 0.95);
			if (!(num5 < from) && !((double)i >= num4))
			{
				double num8 = (double)i * spacing - num;
				Point a = new Point(num8 + num6, size.Height + 1.0);
				Point b = new Point(num8 + num + num7, -1.0);
				double num9 = Math.Min(1.0, num4 - (double)i);
				if (num9 < 1.0)
				{
					b = new Point(a.X + (b.X - a.X) * num9, a.Y + (b.Y - a.Y) * num9);
				}
				bool startClipped = false;
				bool endClipped = false;
				if (!circleInset.HasValue || ClipToCircle(ref a, ref b, c, r, out startClipped, out endClipped))
				{
					PaperPath paperPath = new PaperPath().MoveTo(a.X + offsetX, a.Y + offsetY).LineTo(b.X + offsetX, b.Y + offsetY);
					drawing.Children.Add(new Path
					{
						Data = paperPath.ToGeometry(),
						Stroke = Ui.Brush(color.Opacity(opacity)),
						StrokeThickness = lineWidth,
						StrokeStartLineCap = ((!startClipped) ? PenLineCap.Round : PenLineCap.Flat),
						StrokeEndLineCap = ((!endClipped) ? PenLineCap.Round : PenLineCap.Flat),
						IsHitTestVisible = false
					});
				}
			}
		}
	}

	private static bool ClipToCircle(ref Point a, ref Point b, Point c, double r, out bool startClipped, out bool endClipped)
	{
		startClipped = (endClipped = false);
		if (r <= 0.0)
		{
			return false;
		}
		double num = b.X - a.X;
		double num2 = b.Y - a.Y;
		double num3 = a.X - c.X;
		double num4 = a.Y - c.Y;
		double num5 = num * num + num2 * num2;
		double num6 = 2.0 * (num3 * num + num4 * num2);
		double num7 = num3 * num3 + num4 * num4 - r * r;
		if (num5 <= 0.0)
		{
			return num7 <= 0.0;
		}
		double num8 = num6 * num6 - 4.0 * num5 * num7;
		if (num8 <= 0.0)
		{
			return false;
		}
		double num9 = Math.Sqrt(num8);
		double num10 = (0.0 - num6 - num9) / (2.0 * num5);
		double num11 = (0.0 - num6 + num9) / (2.0 * num5);
		double num12 = Math.Max(0.0, num10);
		double num13 = Math.Min(1.0, num11);
		if (num13 <= num12)
		{
			return false;
		}
		startClipped = num10 > 0.0;
		endClipped = num11 < 1.0;
		Point point = a;
		a = new Point(point.X + num * num12, point.Y + num2 * num12);
		b = new Point(point.X + num * num13, point.Y + num2 * num13);
		return true;
	}
}
