using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTable
{
	private static readonly Color PickHighlight = PaperColor.Hex("#FFE27A").Opacity(0.55);

	private static PaperFont FontFor(ClickyInkTableFont font, double scale)
	{
		return font switch
		{
			ClickyInkTableFont.Name => ClickyPaperFonts.Hand(17.5 * scale), 
			ClickyInkTableFont.Header => ClickyPaperFonts.Typed(Math.Max(8.0, 9.5 * scale), FontWeights.SemiBold), 
			_ => ClickyPaperFonts.Typed(13.0 * scale, FontWeights.SemiBold), 
		};
	}

	public static double Measure(string text, ClickyInkTableFont font, double scale)
	{
		if (text.Length == 0)
		{
			return 0.0;
		}
		using CanvasTextFormat textFormat = FontFor(font, scale).TextFormat();
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, textFormat, 100000f, 100000f);
		return canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
	}

	public static ClickyInkTableLayout Layout(ClickyInkDocument.Block source, double width)
	{
		return ClickyInkTableLayout.Make(source.Columns, ((IEnumerable<ClickyInkDocument.Item>)source.Items).Select((Func<ClickyInkDocument.Item, IReadOnlyList<string>>)((ClickyInkDocument.Item i) => i.Cells)).ToList(), width, Measure);
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkTableLayout clickyInkTableLayout = Layout(block.Source, width);
		double fontScale = clickyInkTableLayout.FontScale;
		StackPanel stackPanel = new StackPanel
		{
			Width = width,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(0.0, InkVerticalAlignment.Center)
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		for (int i = 0; i < clickyInkTableLayout.Widths.Count; i++)
		{
			InkTextView inkTextView = InkTextView.Plain((i < block.Source.Columns.Count) ? block.Source.Columns[i].ToUpperInvariant() : "", ClickyPaperFonts.Typed(Math.Max(8.0, 9.5 * fontScale), FontWeights.SemiBold), style.Quiet).With(delegate(InkTextView t)
			{
				t.Tracking = 0.4;
				t.LineLimit = 2;
				t.MinimumScale = 0.8;
			});
			inkTextView.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			clickyInkHStack.Children.Add(Cell(inkTextView, clickyInkTableLayout.Widths[i]));
		}
		stackPanel.Children.Add(clickyInkHStack);
		stackPanel.Children.Add(new Border
		{
			Height = 1.2,
			Background = Ui.Brush(style.Ink.Opacity(0.3))
		});
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			stackPanel.Children.Add(ClickyInkGrow.Around(Row(item, clickyInkTableLayout, clock, style, width), clock, item.LandsAt, item.VanishesAt));
		}
		return stackPanel;
	}

	private static FrameworkElement Cell(FrameworkElement content, double width)
	{
		content.HorizontalAlignment = HorizontalAlignment.Left;
		return new Grid
		{
			Width = Math.Max(0.0, width),
			Children = { (UIElement)content }
		};
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Item item, ClickyInkTableLayout layout, double clock, ClickyInkStyle style, double width)
	{
		bool flag = item.Source.Mark == ClickyInkDocument.Mark.Pick;
		double? goneAt = item.GoneAt;
		bool num;
		if (goneAt.HasValue)
		{
			double valueOrDefault = goneAt.GetValueOrDefault();
			num = clock >= valueOrDefault;
		}
		else
		{
			num = item.Source.Mark == ClickyInkDocument.Mark.Gone;
		}
		bool flag2 = num;
		double fontScale = layout.FontScale;
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(0.0, InkVerticalAlignment.FirstBaseline)
		{
			Margin = new Thickness(0.0, 7.0, 0.0, 7.0)
		};
		for (int i = 0; i < layout.Widths.Count; i++)
		{
			FrameworkElement frameworkElement;
			if (i < item.Cells.Count)
			{
				ClickyInkLiveNote.Words words = item.Cells[i];
				bool flag3 = ClickyInkTableRules.IsCrossingOut(words, clock);
				PaperFont font = ((i == 0) ? ClickyPaperFonts.Hand(17.5 * fontScale) : ClickyPaperFonts.Typed(13.0 * fontScale, flag ? FontWeights.SemiBold : FontWeights.Normal));
				InkTextView inkTextView = ClickyInkWordsView.Make(words, clock, style, font, pencil: false, flag2 ? new Color?(style.Quiet) : ((Color?)null), null);
				if (flag2)
				{
					inkTextView.Strikethrough = style.Fresh;
				}
				inkTextView.LineLimit = ((!layout.Rigid[i]) ? (flag3 ? 6 : 3) : ((!flag3) ? 1 : 2));
				inkTextView.MinimumScale = (layout.Rigid[i] ? 0.7 : 0.85);
				frameworkElement = inkTextView;
			}
			else
			{
				frameworkElement = InkTextView.Plain("", ClickyPaperFonts.Typed(13.0 * fontScale, null), style.Ink);
			}
			frameworkElement.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			clickyInkHStack.Children.Add(Cell(frameworkElement, layout.Widths[i]));
		}
		Grid grid = new Grid();
		if (flag)
		{
			double num2 = ClickyInkClock.Progress(clock, item.LandsAt + 0.3, item.LandsAt + 0.8);
			ClickyShapeView clickyShapeView = ClickyShapeView.Fill(ClickyShape.RoundedRectangle(3.0), PickHighlight);
			clickyShapeView.Width = Math.Max(0.0, width * num2);
			clickyShapeView.Height = 20.0;
			clickyShapeView.HorizontalAlignment = HorizontalAlignment.Left;
			clickyShapeView.VerticalAlignment = VerticalAlignment.Center;
			clickyShapeView.Transformed(0.0, 0.0, -0.6, 1.0, null);
			grid.Children.Add(clickyShapeView);
		}
		grid.Children.Add(clickyInkHStack);
		return new StackPanel
		{
			Children = 
			{
				(UIElement)grid,
				(UIElement)new Border
				{
					Height = 1.0,
					Background = Ui.Brush(style.Ink.Opacity(0.09))
				}
			}
		};
	}
}
