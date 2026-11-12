using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperFileChip : Grid
{
	public ClickyPaperFileChip(string name, ClickyPaperFileKind? kind = null, double height = 24.0, double fontSize = 11.0)
	{
		ClickyPaperFileKind kind2 = kind ?? ClickyPaperFileKinds.ForFileName(name);
		base.Height = height;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.IsHitTestVisible = false;
		PaperSurface item = new PaperSurface(ClickyShape.RoundedRectangle(6.0), PaperFill.Of(PaperColor.White.Opacity(0.88)), null).WithShadow(PaperColor.Black.Opacity(0.08), 0.75, 0.0, 1.0).WithOutline(new PaperStroke(PaperColor.Hex("#2B2620").Opacity(0.28)));
		base.Children.Add(item);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			Margin = new Thickness(6.0, 0.0, 9.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		PaperDrawing paperDrawing = new PaperDrawing().Sized(11.0, 14.0);
		Rect rect = new Rect(0f, 0f, 11f, 14f);
		paperDrawing.Fill(new PaperPath().AddRoundedRect(rect, 2.0), PaperColor.White);
		PaperPath p = new PaperPath().MoveTo(0.0, 9.0).LineTo(11.0, 9.0).LineTo(11.0, 12.0)
			.QuadTo(new Point(9f, 14f), new Point(11f, 14f))
			.LineTo(2.0, 14.0)
			.QuadTo(new Point(0f, 12f), new Point(0f, 14f))
			.Close();
		Color[] array = kind2.FootColors();
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			MappingMode = BrushMappingMode.Absolute,
			StartPoint = new Point(0f, 9f),
			EndPoint = new Point(0f, 14f)
		};
		linearGradientBrush.GradientStops.Add(new GradientStop
		{
			Offset = 0.0,
			Color = array[0]
		});
		linearGradientBrush.GradientStops.Add(new GradientStop
		{
			Offset = 1.0,
			Color = array[1]
		});
		paperDrawing.Fill(p, linearGradientBrush);
		paperDrawing.Stroke(new PaperPath().AddRoundedRect(rect, 2.0), new PaperStroke(PaperColor.Black.Opacity(0.22)));
		stackPanel.Children.Add(paperDrawing);
		stackPanel.Children.Add(PaperText.Line(name, ClickyPaperFonts.Typed(fontSize, FontWeights.SemiBold), PaperColor.Hex("#2B2620")));
		base.Children.Add(stackPanel);
	}
}
