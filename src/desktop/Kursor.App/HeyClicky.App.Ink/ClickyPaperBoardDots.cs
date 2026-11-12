using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperBoardDots : Grid
{
	public const double Spacing = 16.0;

	public const double Radius = 1.1;

	public const double DotOpacity = 0.11;

	public ClickyPaperBoardDots(Color? color = null, bool followsResizing = false)
	{
		ClickyPaperBoardDots clickyPaperBoardDots = this;
		base.IsHitTestVisible = false;
		Color c = color ?? PaperColor.Black.Opacity(0.11);
		double inset = 6.4;
		if (followsResizing)
		{
			Path rows = new Path
			{
				Stroke = Ui.Brush(c),
				StrokeThickness = 2.2,
				StrokeDashArray = new DoubleCollection { 0.0, 7.2727272727272725 },
				StrokeDashCap = PenLineCap.Round,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round,
				IsHitTestVisible = false
			};
			base.Children.Add(new Canvas
			{
				IsHitTestVisible = false,
				Children = { (UIElement)rows }
			});
			Size drawn = default(Size);
			base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				Size newSize = e.NewSize;
				if (newSize.Width > drawn.Width || newSize.Height > drawn.Height)
				{
					drawn = new Size(newSize.Width + 480.0, newSize.Height + 480.0);
					GeometryGroup geometryGroup = new GeometryGroup();
					for (double num = 16.0; num <= Last(drawn.Height); num += 16.0)
					{
						geometryGroup.Children.Add(new LineGeometry
						{
							StartPoint = new Point(16.0, num),
							EndPoint = new Point(Last(drawn.Width), num)
						});
					}
					rows.Data = geometryGroup;
				}
				clickyPaperBoardDots.Clip = new RectangleGeometry
				{
					Rect = new Rect(0.0, 0.0, Math.Max(0.0, Last(newSize.Width) + 1.1 + 0.5), Math.Max(0.0, Last(newSize.Height) + 1.1 + 0.5))
				};
			};
			return;
		}
		PaperDrawing drawing = new PaperDrawing();
		base.Children.Add(drawing);
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			drawing.Children.Clear();
			PaperPath paperPath = new PaperPath();
			for (double num2 = 16.0; num2 <= e.NewSize.Height - inset; num2 += 16.0)
			{
				for (double num3 = 16.0; num3 <= e.NewSize.Width - inset; num3 += 16.0)
				{
					paperPath.AddEllipse(new Rect(num3 - 1.1, num2 - 1.1, 2.2, 2.2));
				}
			}
			drawing.Fill(paperPath, c);
		};
		double Last(double length)
		{
			return Math.Floor((length - inset) / 16.0) * 16.0;
		}
	}
}
