using System.Collections.Generic;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class LiveLineIconStack : StackPanel
{
	public LiveLineIconStack(IReadOnlyList<ClickyLiveLineIcon> icons, int more, double size, Color ink)
	{
		base.Orientation = Orientation.Horizontal;
		base.IsHitTestVisible = false;
		AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
		for (int i = 0; i < icons.Count; i++)
		{
			base.Children.Add(Disc(icons[i], size, (i == 0) ? 0.0 : ((0.0 - size) * 0.28)));
		}
		if (more > 0)
		{
			TextBlock textBlock = Ui.Text($"+{more}", size * 0.62, FontWeights.SemiBold, ink.WithAlpha(0.55));
			textBlock.Margin = new Thickness(size * 0.42, 0.0, 0.0, 0.0);
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			base.Children.Add(textBlock);
		}
	}

	private static FrameworkElement Disc(ClickyLiveLineIcon icon, double size, double leading)
	{
		double num = size * 0.66;
		string host = icon.Host;
		FrameworkElement frameworkElement = ((host != null) ? ClickyInkObjectKit.Favicon("https://" + host, num, Icons.Glyph("\ue774", size * 0.5, Color.FromArgb(102, 0, 0, 0))) : IntegrationLogos.Artwork(icon.Toolkit, icon.Name, num, size * 0.45, icon.LogoUrl));
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		frameworkElement.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, 0.0, num, num)
		};
		return new Grid
		{
			Width = size,
			Height = size,
			Margin = new Thickness(leading, 0.0, 0.0, 0.0),
			Children = 
			{
				(UIElement)new Ellipse
				{
					Fill = new SolidColorBrush(Colors.White),
					Stroke = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
					StrokeThickness = 0.5
				},
				(UIElement)frameworkElement
			}
		};
	}
}
