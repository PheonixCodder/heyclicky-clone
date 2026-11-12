using System;
using System.Linq;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public abstract record PaperFill
{
	public sealed record Solid(Color Color) : PaperFill();

	public sealed record Linear((double Offset, Color Color)[] Stops, Point Start, Point End) : PaperFill();

	public static PaperFill Of(Color color)
	{
		return new Solid(color);
	}

	public static PaperFill Vertical(params (double Offset, Color Color)[] stops)
	{
		return new Linear(stops, new Point(0.5, 0.0), new Point(0.5, 1.0));
	}

	public static PaperFill Horizontal(params (double Offset, Color Color)[] stops)
	{
		return new Linear(stops, new Point(0.0, 0.5), new Point(1.0, 0.5));
	}

	public static PaperFill Evenly(Point start, Point end, params Color[] colors)
	{
		return new Linear(colors.Select((Color c, int i) => ((colors.Length == 1) ? 0.0 : ((double)i / (double)(colors.Length - 1)), c: c)).ToArray(), start, end);
	}

	internal ICanvasBrush Brush(ICanvasResourceCreator creator, Rect frame)
	{
		if (!(this is Solid solid))
		{
			if (this is Linear linear)
			{
				return new CanvasLinearGradientBrush(creator, linear.Stops.Select(delegate((double Offset, Color Color) st)
				{
					CanvasGradientStop result = default(CanvasGradientStop);
					result.Position = (float)st.Offset;
					result.Color = st.Color;
					return result;
				}).ToArray(), CanvasEdgeBehavior.Clamp, CanvasAlphaMode.Premultiplied)
				{
					StartPoint = new Vector2((float)(frame.X + linear.Start.X * frame.Width), (float)(frame.Y + linear.Start.Y * frame.Height)),
					EndPoint = new Vector2((float)(frame.X + linear.End.X * frame.Width), (float)(frame.Y + linear.End.Y * frame.Height))
				};
			}
			throw new InvalidOperationException();
		}
		return new CanvasSolidColorBrush(creator, solid.Color);
	}
}
