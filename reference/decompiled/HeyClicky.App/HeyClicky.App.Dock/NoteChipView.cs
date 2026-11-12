using System.Collections.Generic;
using HeyClicky.App.Drive;
using HeyClicky.App.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class NoteChipView : Grid
{
	private static readonly Dictionary<string, WriteableBitmap> Pictures = new Dictionary<string, WriteableBitmap>();

	public NoteChipView(string paperKey, string symbol, int chipSeed, int tapeSeed, double size, bool showsTape = true)
	{
		NoteChipView noteChipView = this;
		base.Width = (base.Height = size);
		base.IsHitTestVisible = false;
		base.VerticalAlignment = VerticalAlignment.Top;
		ArcPaper paper = new ArcPaper(NotePaperColors.Stock(paperKey));
		double frame = size * 1.5;
		double angle = tapeSeed % 7 - 3;
		TextBlock textBlock = ClickyInkIcon.Glyph(symbol, 11.0 * size / 24.0, paper.Deep);
		FrameworkElement frameworkElement;
		if ((object)textBlock != null)
		{
			textBlock.FontWeight = FontWeights.SemiBold;
			frameworkElement = textBlock;
		}
		else
		{
			frameworkElement = ArcSymbols.Make(symbol, 11.0 * size / 24.0, paper.Deep, bold: false);
		}
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
		frameworkElement.RenderTransform = new RotateTransform
		{
			Angle = angle
		};
		bool painted = false;
		base.Loaded += delegate
		{
			if (!painted)
			{
				painted = true;
				double num2 = noteChipView.XamlRoot?.RasterizationScale ?? 1.0;
				string key = $"{paper.TitleWash}|{paper.Deep}|{size}|{num2}|{chipSeed}|{tapeSeed}|{showsTape}";
				if (!Pictures.TryGetValue(key, out WriteableBitmap value))
				{
					value = DriveArt.HandoffChip(null, paper.TitleWash, paper.Deep, size, frame, num2 * 2.0, chipSeed, tapeSeed, showsTape);
					if ((object)value != null)
					{
						Pictures[key] = value;
					}
				}
				if ((object)value != null)
				{
					noteChipView.Children.Insert(0, new Image
					{
						Source = value,
						Width = frame,
						Height = frame,
						Margin = new Thickness((0.0 - (frame - size)) / 2.0),
						Stretch = Stretch.Fill
					});
				}
			}
		};
		base.Children.Add(frameworkElement);
	}
}
