using HeyClicky.App.Drive;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class PaperStampView : Grid
{
	public PaperStampView(string word, Color color, double fontSize, double degrees)
	{
		PaperStampView paperStampView = this;
		base.IsHitTestVisible = false;
		base.VerticalAlignment = VerticalAlignment.Center;
		base.Loaded += delegate
		{
			double num = paperStampView.XamlRoot?.RasterizationScale ?? 1.0;
			var (writeableBitmap, num2, num3) = DriveArt.Stamp(word.ToUpperInvariant(), color, fontSize, num * 2.0);
			if ((object)writeableBitmap != null)
			{
				paperStampView.Width = num2;
				paperStampView.Height = num3;
				paperStampView.Children.Add(new Image
				{
					Source = writeableBitmap,
					Width = num2,
					Height = num3,
					Stretch = Stretch.Fill
				});
				paperStampView.RenderTransform = new RotateTransform
				{
					Angle = degrees,
					CenterX = num2 / 2.0,
					CenterY = num3 / 2.0
				};
			}
		};
	}
}
