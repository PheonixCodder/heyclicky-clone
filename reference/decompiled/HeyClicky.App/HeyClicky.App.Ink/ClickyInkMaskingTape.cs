using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkMaskingTape
{
	public static FrameworkElement Make(double width = 58.0, double angle = -3.0, Color? color = null)
	{
		Color color2 = color ?? PaperColor.Hex("#EEE2C0");
		InkPaperSheet item = new InkPaperSheet($"tape|{color2}", new ClickyInkTornStrip(), PaperFill.Of(color2.Opacity(0.7)), 0.3, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.05), 0.6, 0.0, 0.4)
		}, null, PaperFill.Vertical((0.0, PaperColor.White.Opacity(0.25)), (1.0, PaperColor.White.Opacity(0.0))));
		Grid obj = new Grid
		{
			Width = width,
			Height = 14.0,
			IsHitTestVisible = false,
			Children = { (UIElement)item }
		};
		obj.Transformed(0.0, 0.0, angle * 0.6, 1.0, null);
		AutomationProperties.SetAccessibilityView(obj, AccessibilityView.Raw);
		return obj;
	}
}
