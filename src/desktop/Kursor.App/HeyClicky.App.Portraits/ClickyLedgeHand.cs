using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HeyClicky.App.Portraits;

public static class ClickyLedgeHand
{
	public static FrameworkElement Create(PortraitAppearance a, double w, double h)
	{
		Grid grid = new Grid
		{
			Width = w,
			Height = h
		};
		if (a.Pack == PortraitPack.ClassicClouds)
		{
			Path path = new Path
			{
				Data = PortraitArt.LedgeHand(w, h).ToXaml(),
				Fill = Ui.Brush(a.Hair)
			};
			WriteableBitmap writeableBitmap = PortraitWash.For(a, null);
			if ((object)writeableBitmap != null)
			{
				path.Fill = new ImageBrush
				{
					ImageSource = writeableBitmap,
					Stretch = Stretch.Fill
				};
			}
			grid.Children.Add(path);
		}
		else if (a.Pack.IsCloud())
		{
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush
			{
				StartPoint = new Point(0f, 0f),
				EndPoint = new Point(1f, 1f)
			};
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.87),
				Offset = 0.0
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.55),
				Offset = 0.5
			});
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Color = a.HairColor.CloudFill(0.12),
				Offset = 1.0
			});
			grid.Children.Add(new Path
			{
				Data = PortraitArt.LedgeHand(w, h).ToXaml(),
				Fill = linearGradientBrush,
				Stroke = Ui.Brush(PortraitData.Ink),
				StrokeThickness = 1.2
			});
		}
		else
		{
			grid.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(h / 2.0),
				Background = Ui.Brush(a.Skin),
				BorderBrush = Ui.Brush(Colors.Black),
				BorderThickness = new Thickness(1.5)
			});
		}
		return grid;
	}
}
