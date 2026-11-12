using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkLinkPreviewPlate
{
	public static string? WorkerBaseUrl { get; set; }

	public static FrameworkElement Make(string urlString, double iconSize = 40.0, Color? plate = null)
	{
		FrameworkElement item = InkViews.Symbol("link", iconSize * 0.62, PaperColor.Hex("#4A7CF0"), bold: true) ?? new Grid();
		Grid grid = new Grid
		{
			Width = iconSize,
			Height = iconSize,
			Children = { (UIElement)item }
		};
		FrameworkElement frameworkElement = grid;
		string text = FaviconUrl(urlString);
		if (text != null)
		{
			frameworkElement = IntegrationLogos.Remote(text, iconSize, grid);
		}
		frameworkElement.Width = iconSize;
		frameworkElement.Height = iconSize;
		frameworkElement.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, 0.0, iconSize, iconSize)
		};
		IClickyShape shape = ClickyShape.RoundedRectangle(iconSize * 0.22);
		PaperFill fill = PaperFill.Of(PaperColor.Clear);
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.12), 3.0, 0.0, 1.5)
		};
		InkPaperSheet item2 = new InkPaperSheet("link-plate-icon", shape, fill, null, shadows)
		{
			Width = iconSize,
			Height = iconSize
		};
		Grid item3 = new Grid
		{
			Width = iconSize,
			Height = iconSize,
			CornerRadius = new CornerRadius(iconSize * 0.22),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)frameworkElement }
		};
		Grid item4 = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)item2 }
		};
		return new Grid
		{
			Background = Ui.Brush(plate ?? Color.FromArgb(byte.MaxValue, 242, 242, 242)),
			Children = 
			{
				(UIElement)item4,
				(UIElement)item3
			}
		};
	}

	private static string? FaviconUrl(string urlString)
	{
		string workerBaseUrl = WorkerBaseUrl;
		if (workerBaseUrl == null || workerBaseUrl.Length <= 0 || !Uri.TryCreate(urlString, UriKind.Absolute, out Uri result) || result.Host.Length == 0)
		{
			return null;
		}
		return workerBaseUrl.TrimEnd('/') + "/favicon?host=" + Uri.EscapeDataString(result.Host.ToLowerInvariant()) + "&sz=128";
	}
}
