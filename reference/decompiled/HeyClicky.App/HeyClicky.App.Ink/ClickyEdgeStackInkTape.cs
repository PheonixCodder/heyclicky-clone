using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackInkTape : Grid
{
	public ClickyEdgeStackInkTape(ClickyEdgeStackInkReading.State state)
	{
		base.IsHitTestVisible = false;
		AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
		Color c = ClickyEdgeStackInkLook.TapeColor(state);
		base.Margin = new Thickness(-4.0, 10.0, 4.0, 10.0);
		base.Width = 22.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		PaperSurface item = new PaperSurface(ClickyShape.Rectangle, PaperFill.Of(c.Opacity(0.92)), 0.35).WithWash(PaperFill.Evenly(new Point(0.0, 0.5), new Point(1.0, 0.5), PaperColor.White.Opacity(0.22), PaperColor.Clear, PaperColor.Black.Opacity(0.06)));
		base.Children.Add(item);
		FrameworkElement frameworkElement = Glyph(state, null);
		if ((object)frameworkElement != null)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			frameworkElement.Margin = new Thickness(0.0, 108.0, 0.0, 0.0);
			Grid grid = new Grid
			{
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			FrameworkElement frameworkElement2 = Glyph(state, PaperColor.Black.Opacity(0.18));
			if ((object)frameworkElement2 != null)
			{
				frameworkElement2.VerticalAlignment = VerticalAlignment.Top;
				frameworkElement2.HorizontalAlignment = HorizontalAlignment.Center;
				frameworkElement2.Margin = new Thickness(0.0, 108.5, 0.0, 0.0);
				grid.Children.Add(frameworkElement2);
			}
			grid.Children.Add(frameworkElement);
			base.Children.Add(grid);
		}
	}

	private static FrameworkElement? Glyph(ClickyEdgeStackInkReading.State state, Color? color = null)
	{
		Color color2 = color ?? PaperColor.White;
		switch (state)
		{
		case ClickyEdgeStackInkReading.State.Writing:
		{
			TextBlock? textBlock = ClickyInkIcon.Glyph("pencil", 15.0, color2, bold: true);
			textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
			textBlock.RenderTransform = new RotateTransform
			{
				Angle = -20.0
			};
			return textBlock;
		}
		case ClickyEdgeStackInkReading.State.NeedsYou:
			return PaperText.Line("?", ClickyPaperFonts.Rounded(18.0, FontWeights.ExtraBold), color2);
		case ClickyEdgeStackInkReading.State.Ready:
			return ClickyInkIcon.Glyph("checkmark", 14.0, color2, bold: true);
		default:
			return null;
		}
	}
}
