using System.Collections.Generic;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkReceipt
{
	private static readonly Color Ink = PaperColor.Hex("#2A2A2A");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		double num = ClickyInkReceiptRules.Width(block.Source);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0,
			Width = num,
			Padding = new Thickness(16.0, 14.0, 16.0, 20.0)
		};
		InkTextView inkTextView = InkTextView.Plain(((block.Words.Current.Length == 0) ? "Receipt" : block.Words.Current).ToUpperInvariant(), ClickyInkObjectFonts.Mono(11.0, FontWeights.Bold), Ink).With(delegate(InkTextView t)
		{
			t.Tracking = 1.5;
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		stackPanel.Children.Add(inkTextView);
		PaperFont font = ClickyInkObjectFonts.Mono(11.5, null);
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			if (ClickyInkReceiptRules.IsTotalLine(item.Source) || clock < item.LandsAt)
			{
				continue;
			}
			double? goneAt = item.GoneAt;
			int num2;
			if (goneAt.HasValue)
			{
				double valueOrDefault = goneAt.GetValueOrDefault();
				num2 = ((clock >= valueOrDefault) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			bool flag = (byte)num2 != 0;
			InkTextView inkTextView2 = InkTextView.Plain((item.Source.Cells.Count > 0) ? item.Source.Cells[0] : item.Source.Text, font, Ink).With(delegate(InkTextView t)
			{
				t.LineLimit = 3;
			});
			if (flag)
			{
				inkTextView2.Strikethrough = style.Fresh;
			}
			InkTextView inkTextView3 = null;
			bool flag2 = false;
			if (item.Cells.Count > 1)
			{
				ClickyInkLiveNote.Words words = item.Cells[1];
				flag2 = words.Current.CharacterCount() > 12;
				inkTextView3 = new InkTextView(ClickyInkWords.Text(words, clock, style, pencil: false, Ink, null), font, Ink);
				if (flag2)
				{
					inkTextView3.LineLimit = 2;
					inkTextView3.TextAlignment = TextAlignment.Right;
				}
				if (flag)
				{
					inkTextView3.Strikethrough = style.Fresh;
				}
			}
			stackPanel.Children.Add(new ClickyInkReceiptLine(inkTextView2, inkTextView3, flag2 ? new double?((num - 32.0) * 0.48) : ((double?)null)));
		}
		string text = ClickyInkReceiptRules.Total(block.Source);
		if (text != null)
		{
			double num3;
			if (block.Items.Count <= 0)
			{
				num3 = block.LandsAt;
			}
			else
			{
				List<ClickyInkLiveNote.Item> items = block.Items;
				num3 = items[items.Count - 1].LandsAt + 0.3;
			}
			if (clock >= num3)
			{
				stackPanel.Children.Add(new Border
				{
					Height = 1.0,
					Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
					Background = Ui.Brush(Ink.Opacity(0.6))
				});
				InkTextView content = InkTextView.Plain(text, ClickyInkObjectFonts.Mono(15.0, FontWeights.ExtraBold), Ink).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.7;
				});
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(4), style.Fresh.Opacity(0.7), 1.8);
				clickyShapeView.Margin = new Thickness(-5.0);
				stackPanel.Children.Add(new ClickyInkHStack(8.0, InkVerticalAlignment.Center, InkTextView.Plain("TOTAL", ClickyInkObjectFonts.Mono(12.0, FontWeights.Bold), Ink), new ClickyInkSpacer(), new ClickyInkOverlay(content, clickyShapeView)));
			}
		}
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel).Behind(new ClickyInkShadowedShape(new ClickyInkZigzagFoot(), "zigzag", PaperColor.Hex("#FDFDFB"), new PaperShadow(PaperColor.Black.Opacity(0.12), 3.0, 0.0, 2.0)));
		clickyInkOverlay.Transformed(0.0, 0.0, 1.1, 1.0, null);
		FrameworkElement frameworkElement = ClickyInkObjectKit.Leading(clickyInkOverlay, contentWidth);
		frameworkElement.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		return frameworkElement;
	}
}
