using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkShadowedShape : Panel
{
	private readonly IClickyShape shape;

	private readonly string shapeKey;

	private readonly Brush? fill;

	private readonly PaperStroke? stroke;

	private readonly PaperShadow? shadow;

	private readonly Image shadowImage = new Image
	{
		Stretch = Stretch.Fill,
		IsHitTestVisible = false
	};

	private readonly Path fillPath = new Path
	{
		IsHitTestVisible = false
	};

	private readonly Path strokePath = new Path
	{
		IsHitTestVisible = false
	};

	private Size drawnSize;

	public ClickyInkShadowedShape(IClickyShape shape, string shapeKey, Brush? fill, PaperShadow? shadow = null, PaperStroke? stroke = null)
	{
		this.shape = shape;
		this.shapeKey = shapeKey;
		this.fill = fill;
		this.stroke = stroke;
		this.shadow = shadow;
		base.IsHitTestVisible = false;
		base.Children.Add(shadowImage);
		base.Children.Add(fillPath);
		base.Children.Add(strokePath);
		fillPath.Fill = fill;
		if ((object)stroke != null)
		{
			strokePath.Stroke = Ui.Brush(stroke.Color);
			strokePath.StrokeThickness = stroke.Width;
			strokePath.StrokeStartLineCap = (strokePath.StrokeEndLineCap = stroke.Cap);
			strokePath.StrokeLineJoin = stroke.Join;
		}
	}

	public ClickyInkShadowedShape(IClickyShape shape, string shapeKey, Color fill, PaperShadow? shadow = null, PaperStroke? stroke = null)
		: this(shape, shapeKey, Ui.Brush(fill), shadow, stroke)
	{
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(0f, 0f));
		}
		return new Size(0f, 0f);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (finalSize != drawnSize)
		{
			drawnSize = finalSize;
			Rect rect = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height);
			PaperPath paperPath = shape.PathIn(rect);
			fillPath.Data = paperPath.ToGeometry();
			if ((object)stroke != null)
			{
				strokePath.Data = shape.PathIn(rect).ToGeometry();
			}
			PaperShadow paperShadow = shadow;
			if ((object)paperShadow != null && finalSize.Width > 0.0 && finalSize.Height > 0.0)
			{
				var (source, num) = ClickyInkShadowPictures.Picture(shape, shapeKey, finalSize, paperShadow);
				shadowImage.Source = source;
				shadowImage.Tag = num;
			}
		}
		foreach (UIElement child in base.Children)
		{
			if (child == shadowImage)
			{
				PaperShadow paperShadow2 = shadow;
				if ((object)paperShadow2 != null && shadowImage.Tag is double num2)
				{
					child.Arrange(new Rect(paperShadow2.X - num2, paperShadow2.Y - num2, finalSize.Width + 2.0 * num2, finalSize.Height + 2.0 * num2));
					continue;
				}
			}
			child.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		}
		return finalSize;
	}
}
