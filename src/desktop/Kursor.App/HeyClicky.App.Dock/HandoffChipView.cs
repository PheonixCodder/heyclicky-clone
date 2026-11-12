using HeyClicky.App.Drive;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class HandoffChipView : Grid
{
	public HandoffChipView(HandoffGlyph glyph, string railKey, double size = 32.0, PaperStock? stock = null, bool showsTape = true)
	{
		HandoffChipView handoffChipView = this;
		base.Width = (base.Height = size);
		base.IsHitTestVisible = false;
		ArcPaper paper = new ArcPaper(stock ?? NotePaperColors.Stock(NotePaperColors.Key(railKey)));
		int seed = NotePaperColors.Seed(railKey);
		double frame = size * 1.5;
		base.Loaded += delegate
		{
			if (handoffChipView.Children.Count <= 0)
			{
				double num2 = handoffChipView.XamlRoot?.RasterizationScale ?? 1.0;
				string glyphPath = HandoffGlyphs.Path(glyph);
				Color titleWash = paper.TitleWash;
				Color deep = paper.Deep;
				double chipSize = size;
				double frameSize = frame;
				double scale = num2 * 2.0;
				int seed2 = seed;
				bool showsTape2 = showsTape;
				WriteableBitmap writeableBitmap = DriveArt.HandoffChip(glyphPath, titleWash, deep, chipSize, frameSize, scale, seed2, null, showsTape2);
				if ((object)writeableBitmap != null)
				{
					handoffChipView.Children.Add(new Image
					{
						Source = writeableBitmap,
						Width = frame,
						Height = frame,
						Margin = new Thickness((0.0 - (frame - size)) / 2.0),
						Stretch = Stretch.Fill
					});
				}
			}
		};
	}
}
