using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperSticker : Grid
{
	public ClickyPaperSticker(ClickyPaperStickerKind kind, double diameter = 42.0)
	{
		base.Width = (base.Height = diameter);
		base.IsHitTestVisible = false;
		PaperSurface item = new PaperSurface(ClickyShape.Circle, PaperFill.Of(kind.Fill()), null).WithShadow(PaperColor.Black.Opacity(0.18), 0.5, 0.0, 1.0).WithShadow(PaperColor.Hex("#141428").Opacity(0.18), 3.0, 0.0, 3.0);
		base.Children.Add(item);
		RadialGradientBrush radialGradientBrush = new RadialGradientBrush
		{
			Center = new Point(0.34, 0.28),
			GradientOrigin = new Point(0.34, 0.28),
			RadiusX = 0.42,
			RadiusY = 0.42,
			MappingMode = BrushMappingMode.RelativeToBoundingBox
		};
		radialGradientBrush.GradientStops.Add(new GradientStop
		{
			Offset = 0.0,
			Color = PaperColor.White.Opacity(0.55)
		});
		radialGradientBrush.GradientStops.Add(new GradientStop
		{
			Offset = 1.0,
			Color = PaperColor.White.Opacity(0.0)
		});
		base.Children.Add(new Ellipse
		{
			Fill = radialGradientBrush
		});
		base.Children.Add(ClickyShapeView.StrokeBorder(ClickyShape.Circle, new PaperStroke(PaperColor.White.Opacity(0.4), 2.0)));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (kind == ClickyPaperStickerKind.Live)
		{
			Grid grid = new Grid
			{
				Width = 5.0,
				Height = 5.0,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			grid.Children.Add(new Ellipse
			{
				Fill = Ui.Brush(PaperColor.White)
			});
			grid.Children.Add(new Ellipse
			{
				Stroke = Ui.Brush(PaperColor.White.Opacity(0.35)),
				StrokeThickness = 2.0,
				Margin = new Thickness(-1.0)
			});
			stackPanel.Children.Add(grid);
		}
		double size = diameter * 0.238;
		PaperFont paperFont = ClickyPaperFonts.Typed(size, FontWeights.Bold);
		TextBlock textBlock = new TextBlock();
		TextBlock textBlock2 = textBlock;
		textBlock2.Text = kind switch
		{
			ClickyPaperStickerKind.NeedsYou => "needs\nyou", 
			ClickyPaperStickerKind.New => "new", 
			_ => "live", 
		};
		textBlock.TextAlignment = TextAlignment.Center;
		textBlock.Foreground = Ui.Brush(kind.Words());
		textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
		textBlock.LineHeight = ClickyHandLineBreaker.NaturalLineHeight(ClickyPaperFonts.Typed(size, FontWeights.Bold)) - 2.0;
		TextBlock item2 = paperFont.Apply(textBlock);
		stackPanel.Children.Add(item2);
		base.Children.Add(stackPanel);
		base.RenderTransformOrigin = new Point(0.5, 0.5);
		base.RenderTransform = new RotateTransform
		{
			Angle = -9.0
		};
		AutomationProperties.SetName(this, kind switch
		{
			ClickyPaperStickerKind.NeedsYou => "Needs you", 
			ClickyPaperStickerKind.New => "New", 
			_ => "Live", 
		});
	}
}
