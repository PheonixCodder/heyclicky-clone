using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyShapeView : Grid
{
	private readonly Path path = new Path
	{
		IsHitTestVisible = false
	};

	private IClickyShape shape;

	private Brush? fill;

	private PaperStroke? stroke;

	private bool strokeBorder;

	public IClickyShape Shape
	{
		get
		{
			return shape;
		}
		set
		{
			shape = value;
			Rebuild();
		}
	}

	public Brush? FillBrush
	{
		get
		{
			return fill;
		}
		set
		{
			fill = value;
			Apply();
		}
	}

	public PaperStroke? StrokeStyle
	{
		get
		{
			return stroke;
		}
		set
		{
			stroke = value;
			Apply();
			Rebuild();
		}
	}

	public ClickyShapeView(IClickyShape shape, Brush? fill = null, PaperStroke? stroke = null, bool strokeBorder = false)
	{
		this.shape = shape;
		this.fill = fill;
		this.stroke = stroke;
		this.strokeBorder = strokeBorder;
		base.IsHitTestVisible = false;
		base.Children.Add(new Canvas
		{
			IsHitTestVisible = false,
			Children = { (UIElement)path }
		});
		base.SizeChanged += delegate
		{
			Rebuild();
		};
		Apply();
	}

	public static ClickyShapeView Fill(IClickyShape shape, Color color)
	{
		return new ClickyShapeView(shape, Ui.Brush(color));
	}

	public static ClickyShapeView Fill(IClickyShape shape, Brush brush)
	{
		return new ClickyShapeView(shape, brush);
	}

	public static ClickyShapeView Stroke(IClickyShape shape, PaperStroke stroke)
	{
		return new ClickyShapeView(shape, null, stroke);
	}

	public static ClickyShapeView Stroke(IClickyShape shape, Color color, double width)
	{
		return new ClickyShapeView(shape, null, new PaperStroke(color, width));
	}

	public static ClickyShapeView StrokeBorder(IClickyShape shape, PaperStroke stroke)
	{
		return new ClickyShapeView(shape, null, stroke, strokeBorder: true);
	}

	private void Apply()
	{
		path.Fill = fill;
		PaperStroke paperStroke = stroke;
		if ((object)paperStroke != null)
		{
			path.Stroke = Ui.Brush(paperStroke.Color);
			path.StrokeThickness = paperStroke.Width;
			Path obj = path;
			Path obj2 = path;
			PenLineCap penLineCap = (path.StrokeDashCap = paperStroke.Cap);
			PenLineCap strokeStartLineCap = (obj2.StrokeEndLineCap = penLineCap);
			obj.StrokeStartLineCap = strokeStartLineCap;
			path.StrokeLineJoin = paperStroke.Join;
			double[] dash = paperStroke.Dash;
			if (dash != null)
			{
				DoubleCollection doubleCollection = new DoubleCollection();
				double[] array = dash;
				foreach (double num in array)
				{
					doubleCollection.Add(num / Math.Max(paperStroke.Width, 0.0001));
				}
				path.StrokeDashArray = doubleCollection;
			}
			else
			{
				path.StrokeDashArray = null;
			}
		}
		else
		{
			path.Stroke = null;
		}
	}

	private void Rebuild()
	{
		if (base.ActualWidth <= 0.0 && base.ActualHeight <= 0.0 && double.IsNaN(base.Width))
		{
			return;
		}
		double num = ((base.ActualWidth > 0.0) ? base.ActualWidth : (double.IsNaN(base.Width) ? 0.0 : base.Width));
		double num2 = ((base.ActualHeight > 0.0) ? base.ActualHeight : (double.IsNaN(base.Height) ? 0.0 : base.Height));
		Rect rect = new Rect(0.0, 0.0, num, num2);
		if (strokeBorder)
		{
			PaperStroke paperStroke = stroke;
			if ((object)paperStroke != null)
			{
				double num3 = paperStroke.Width / 2.0;
				rect = new Rect(num3, num3, Math.Max(0.0, num - 2.0 * num3), Math.Max(0.0, num2 - 2.0 * num3));
			}
		}
		path.Data = shape.PathIn(rect).ToGeometry();
	}
}
