using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchBar : Grid
{
	public ClickySketchBar(double fraction, double height = 10.0, Color? fill = null, Color? rest = null, long seed = 8L)
	{
		base.Height = height + 4.0;
		base.IsHitTestVisible = false;
		PaperDrawing drawing = new PaperDrawing();
		base.Children.Add(drawing);
		Color f = fill ?? PaperColor.Hex("#7FA6D6");
		Color r = rest ?? PaperColor.Hex("#F1F6FC");
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			drawing.Children.Clear();
			double width = e.NewSize.Width;
			double y = height / 2.0 + 2.0;
			drawing.Stroke(ClickySketchPath.Line(new Point(2.0, y), new Point(width - 2.0, y), seed, 0.5), new PaperStroke(r, height, PenLineCap.Round));
			drawing.Stroke(ClickySketchPath.Line(new Point(2.0, y), new Point(Math.Max(4.0, width * Math.Min(Math.Max(fraction, 0.0), 1.0)), y), seed + 1, 0.5), new PaperStroke(f, height, PenLineCap.Round));
		};
	}
}
