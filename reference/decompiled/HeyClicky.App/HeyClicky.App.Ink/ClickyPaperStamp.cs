using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperStamp : Grid
{
	public Size StampSize { get; }

	public ClickyPaperStamp(ClickyThingStatus status, string? word = null, double fontSize = 9.5, double rotationDegrees = -6.0)
	{
		ClickyPaperTextures.StampImage stampImage = ClickyPaperTextures.Stamp((word ?? status.StampWord()).ToUpperInvariant(), status.StampInk(), fontSize);
		StampSize = stampImage.Size;
		base.Width = stampImage.Size.Width;
		base.Height = stampImage.Size.Height;
		base.IsHitTestVisible = false;
		base.Children.Add(new Image
		{
			Source = stampImage.Bitmap,
			Width = stampImage.Size.Width,
			Height = stampImage.Size.Height,
			Stretch = Stretch.Fill,
			Opacity = 0.9
		});
		base.RenderTransformOrigin = new Point(0.5, 0.5);
		base.RenderTransform = new RotateTransform
		{
			Angle = rotationDegrees
		};
		string text = word ?? status.StampWord();
		AutomationProperties.SetName(this, word ?? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()));
	}

	public static Size SizeOf(ClickyThingStatus status, string? word = null, double fontSize = 9.5)
	{
		return ClickyPaperTextures.Stamp((word ?? status.StampWord()).ToUpperInvariant(), status.StampInk(), fontSize).Size;
	}
}
