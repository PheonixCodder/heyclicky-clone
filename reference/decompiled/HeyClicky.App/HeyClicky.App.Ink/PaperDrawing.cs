using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class PaperDrawing : Canvas
{
	public PaperDrawing()
	{
		base.IsHitTestVisible = false;
	}

	public PaperDrawing Fill(PaperPath p, Color color)
	{
		return Fill(p, Ui.Brush(color));
	}

	public PaperDrawing Fill(PaperPath p, Brush brush)
	{
		base.Children.Add(new Path
		{
			Data = p.ToGeometry(),
			Fill = brush,
			IsHitTestVisible = false
		});
		return this;
	}

	public PaperDrawing Stroke(PaperPath p, PaperStroke s)
	{
		Path path = new Path
		{
			Data = p.ToGeometry(),
			Stroke = Ui.Brush(s.Color),
			StrokeThickness = s.Width,
			StrokeLineJoin = s.Join,
			StrokeStartLineCap = s.Cap,
			StrokeEndLineCap = s.Cap,
			StrokeDashCap = s.Cap,
			IsHitTestVisible = false
		};
		double[] dash = s.Dash;
		if (dash != null)
		{
			DoubleCollection doubleCollection = new DoubleCollection();
			double[] array = dash;
			foreach (double num in array)
			{
				doubleCollection.Add(num / Math.Max(s.Width, 0.0001));
			}
			path.StrokeDashArray = doubleCollection;
		}
		base.Children.Add(path);
		return this;
	}

	public PaperDrawing Sized(double width, double height)
	{
		base.Width = width;
		base.Height = height;
		return this;
	}
}
