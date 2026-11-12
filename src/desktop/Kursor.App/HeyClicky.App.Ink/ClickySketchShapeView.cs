using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchShapeView : Grid
{
	private readonly Color fill;

	private readonly Color pencil;

	private readonly double radius;

	private readonly double lineWidth;

	private readonly long seed;

	private readonly bool isDoubled;

	private readonly bool hasShelf;

	private readonly PaperDrawing drawing = new PaperDrawing();

	public ClickySketchShapeView(Color fill, Color pencil, double radius = 3.0, long seed = 1L, double lineWidth = 1.3, bool isDoubled = true, bool hasShelf = false)
	{
		bool flag = isDoubled;
		bool flag2 = hasShelf;
		this.fill = fill;
		this.pencil = pencil;
		this.radius = radius;
		this.seed = seed;
		this.lineWidth = lineWidth;
		this.isDoubled = flag;
		this.hasShelf = flag2;
		base.IsHitTestVisible = false;
		base.Children.Add(drawing);
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			Draw(e.NewSize);
		};
	}

	private void Draw(Size size)
	{
		drawing.Children.Clear();
		PaperPath p = ClickySketchPath.RoundedRect(new Rect(0.0, 0.0, size.Width, size.Height), radius, seed);
		drawing.Fill(p, fill);
		drawing.Stroke(p, new PaperStroke(pencil, lineWidth, PenLineCap.Flat, PenLineJoin.Round));
		if (isDoubled)
		{
			drawing.Stroke(ClickySketchPath.RoundedRect(new Rect(0.5, 0.3, size.Width - 0.6, size.Height - 0.4), radius, seed + 17, 0.55), new PaperStroke(pencil.Opacity(0.45), lineWidth * 0.7, PenLineCap.Flat, PenLineJoin.Round));
		}
		if (hasShelf)
		{
			drawing.Stroke(ClickySketchPath.Line(new Point(-2.0, size.Height + 2.2), new Point(size.Width + 2.0, size.Height + 2.4), seed + 3, 0.35), new PaperStroke(ClickySketchInk.Shelf, 2.8, PenLineCap.Round));
			drawing.Stroke(ClickySketchPath.Line(new Point(-1.0, size.Height + 5.6), new Point(size.Width + 1.0, size.Height + 5.4), seed + 5, 0.3), new PaperStroke(PaperColor.White.Opacity(0.85), 1.6, PenLineCap.Round));
		}
	}
}
