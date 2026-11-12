using HeyClicky.App.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyBrandMark : Grid
{
	public static readonly (double Offset, Color Color)[] GradientStops = new(double, Color)[3]
	{
		(0.0, PaperColor.White),
		(0.55, PaperColor.Hex("#C2DBFF")),
		(1.0, PaperColor.Hex("#86B6FF"))
	};

	public ClickyBrandMark(double width = 22.0, Color? color = null, double lineWidth = 3.0)
	{
		double num = width * ClickyBrandMarkShape.CropSize.Height / ClickyBrandMarkShape.CropSize.Width;
		base.Width = width;
		base.Height = num;
		base.IsHitTestVisible = false;
		double strokeThickness = lineWidth * width / ClickyBrandMarkShape.CropSize.Width;
		Geometry data = new ClickyBrandMarkShape().PathIn(new Rect(0.0, 0.0, width, num)).ToGeometry();
		Brush stroke;
		if (color.HasValue)
		{
			Color valueOrDefault = color.GetValueOrDefault();
			stroke = Ui.Brush(valueOrDefault);
		}
		else
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				MappingMode = BrushMappingMode.Absolute,
				StartPoint = new Point(0f, 0f),
				EndPoint = new Point(width, num)
			};
			(double, Color)[] gradientStops = GradientStops;
			for (int i = 0; i < gradientStops.Length; i++)
			{
				var (offset, color2) = gradientStops[i];
				linearGradientBrush.GradientStops.Add(new GradientStop
				{
					Offset = offset,
					Color = color2
				});
			}
			stroke = linearGradientBrush;
		}
		base.Children.Add(new Path
		{
			Data = data,
			Stroke = stroke,
			StrokeThickness = strokeThickness,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			IsHitTestVisible = false
		});
	}
}
