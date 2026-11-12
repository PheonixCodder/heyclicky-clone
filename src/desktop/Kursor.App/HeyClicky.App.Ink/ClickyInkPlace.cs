using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkPlace
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.3, block.LandsAt + 0.75);
		Grid grid = new Grid
		{
			Width = 96.0,
			Height = 74.0,
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(PaperColor.Hex("#EEF1E6"))
		};
		grid.Children.Add(Map(block.Source.Text, 96.0, 74.0));
		FrameworkElement frameworkElement = Pin();
		frameworkElement.Transformed(0.0, -18.0 * (1.0 - num) - 4.0, 0.0, 1.0, null);
		frameworkElement.Opacity = ((num > 0.0) ? 1 : 0);
		grid.Children.Add(frameworkElement);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(20.0), pencil: true, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
		}));
		string text = block.Source["note"];
		if (text != null)
		{
			stackPanel.Children.Add(InkTextView.Plain(text, ClickyPaperFonts.Typed(12.0, null), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		string text2 = block.Source["rating"];
		if (text2 != null)
		{
			stackPanel2.Children.Add(InkTextView.Plain("★ " + text2, ClickyPaperFonts.Typed(11.0, FontWeights.SemiBold), PaperColor.Hex("#B7791F")));
		}
		string text3 = block.Source["when"] ?? block.Source["hours"];
		if (text3 != null)
		{
			stackPanel2.Children.Add(InkTextView.Plain(text3, ClickyPaperFonts.Typed(11.0, null), style.Quiet));
		}
		stackPanel.Children.Add(stackPanel2);
		ClickyInkHStack item = new ClickyInkHStack(12.0, InkVerticalAlignment.Center, grid, ClickyInkHStack.Flexible(stackPanel))
		{
			Margin = new Thickness(8.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = 
			{
				(UIElement)ClickyShapeView.Fill(ClickyShape.RoundedRectangle(10.0), PaperColor.White.Opacity(0.55)),
				(UIElement)item,
				(UIElement)ClickyShapeView.Stroke(ClickyShape.RoundedRectangle(10.0), style.Ink.Opacity(0.08), 1.0)
			}
		};
	}

	private static FrameworkElement Map(string text, double width, double height)
	{
		ClickyPaperRandom generator = new ClickyPaperRandom(ClickyPaperRandom.StableSeed(text));
		PaperDrawing paperDrawing = new PaperDrawing().Sized(width, height);
		double x = width * R() * 0.5;
		double y = height * R() * 0.5;
		paperDrawing.Fill(new PaperPath().AddEllipse(new Rect(x, y, 34.0, 26.0)), PaperColor.Hex("#CFE7C4"));
		for (int i = 0; i < 3; i++)
		{
			double y2 = height * R();
			double y3 = height * R();
			double x2 = width * R();
			double y4 = height * R();
			paperDrawing.Stroke(new PaperPath().MoveTo(0.0, y2).QuadTo(new Point(width, y3), new Point(x2, y4)), new PaperStroke(PaperColor.White, 4.0));
		}
		double x3 = width * R();
		double x4 = width * R();
		double x5 = width * R();
		paperDrawing.Stroke(new PaperPath().MoveTo(x3, 0.0).QuadTo(new Point(x4, height), new Point(x5, height / 2.0)), new PaperStroke(PaperColor.Hex("#BFD9F2"), 5.0));
		return paperDrawing;
		double R()
		{
			return (double)(generator.Next() % 1000) / 1000.0;
		}
	}

	private static FrameworkElement Pin()
	{
		Ellipse item = new Ellipse
		{
			Width = 18.919999999999998,
			Height = 18.919999999999998,
			Fill = Ui.Brush(PaperColor.Hex("#E5484D"))
		};
		Grid grid = new Grid
		{
			Width = 18.919999999999998,
			Height = 18.919999999999998,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		grid.Children.Add(item);
		TextBlock textBlock = ClickyInkIcon.Glyph("mappin", 11.0, PaperColor.White, bold: true);
		if ((object)textBlock != null)
		{
			grid.Children.Add(textBlock);
		}
		return grid;
	}
}
