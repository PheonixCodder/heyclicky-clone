using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkEditionView : Grid
{
	private static readonly PaperFont Masthead = new PaperFont(new FontFamily("Georgia"), "Georgia", 38.0, FontWeights.Bold);

	private static readonly PaperFont Headline = new PaperFont(new FontFamily("Georgia"), "Georgia", 25.0, FontWeights.Bold);

	public ClickyInkEditionView(ClickyInkLiveNote note, double clock, int edition, string dateLine, ClickyHomeSpreadPaper? paper = null)
	{
		ClickyHomeSpreadPaper paper2 = paper ?? ClickyHomeSpreadPaper.Note;
		ClickyInkStyle clickyInkStyle = new ClickyInkStyle(PaperColor.Hex("#1E1B16"), PaperColor.Hex("#6B655A"), ClickyPaperInk.ClickyBlue, note.LatestEdit(clock));
		List<ClickyInkGroup> source = note.Pages.SelectMany((ClickyInkLiveNote.Page page) => ClickyInkGroup.Groups(page, clock)).ToList();
		ClickyInkGroup headline = source.FirstOrDefault((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Answer);
		List<ClickyInkGroup> source2 = source.Where((ClickyInkGroup g) => g.Id != headline?.Id).ToList();
		base.Width = 900.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		base.Children.Add(new PaperSurface(ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#F2EDE1")), 0.55).WithShadow(PaperColor.Black.Opacity(0.3), 12.0, 0.0, 5.0));
		StackPanel stackPanel = new StackPanel
		{
			Padding = new Thickness(26.0)
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 4.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		ClickyInkLiveNote.Words title = note.Title;
		InkTextView inkTextView = InkTextView.Plain(((object)title != null) ? ClickyInkParser.Emphasis.Strip(title.Current) : "The Note", Masthead, clickyInkStyle.Ink).With(delegate(InkTextView x)
		{
			x.LineLimit = 1;
			x.MinimumScale = 0.6;
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		stackPanel2.Children.Add(inkTextView);
		stackPanel2.Children.Add(Rule(clickyInkStyle.Ink, 1.4));
		Grid grid = new Grid();
		GridLength[] array = new GridLength[5]
		{
			GridLength.Auto,
			new GridLength(1.0, GridUnitType.Star),
			GridLength.Auto,
			new GridLength(1.0, GridUnitType.Star),
			GridLength.Auto
		};
		foreach (GridLength width in array)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = width
			});
		}
		string[] array2 = new string[3]
		{
			$"EDITION {edition}",
			dateLine.ToUpperInvariant(),
			"BY CLICKY"
		};
		for (int k = 0; k < array2.Length; k++)
		{
			InkTextView inkTextView2 = InkTextView.Plain(array2[k], ClickyPaperFonts.Typed(9.5, FontWeights.Bold), clickyInkStyle.Quiet).With(delegate(InkTextView x)
			{
				x.Tracking = 1.4;
			});
			Grid.SetColumn(inkTextView2, k * 2);
			grid.Children.Add(inkTextView2);
		}
		stackPanel2.Children.Add(grid);
		stackPanel2.Children.Add(Rule(clickyInkStyle.Ink, 0.8));
		Rectangle rectangle = Rule(clickyInkStyle.Ink, 0.8);
		rectangle.Margin = new Thickness(0.0, 1.5, 0.0, 0.0);
		stackPanel2.Children.Add(rectangle);
		stackPanel.Children.Add(stackPanel2);
		if ((object)headline != null)
		{
			ClickyInkLiveNote.Block block = headline.Blocks[0];
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(16.0, InkVerticalAlignment.Top, ClickyInkHStack.Flexible(ClickyInkWordsView.Make(block.Words, clock, clickyInkStyle, Headline, pencil: true, null, null).With(delegate(InkTextView x)
			{
				x.LineSpacing = 2.0;
			})));
			string text = block.Source["big"];
			if (text != null && text.Length > 0)
			{
				StackPanel stackPanel3 = new StackPanel
				{
					Spacing = 2.0
				};
				InkTextView inkTextView3 = InkTextView.Plain(text, ClickyPaperFonts.BigNumber(36.0), clickyInkStyle.Ink);
				inkTextView3.HorizontalAlignment = HorizontalAlignment.Center;
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(5), clickyInkStyle.Fresh.Opacity(0.75), 2.0);
				clickyShapeView.Margin = new Thickness(-5.0);
				stackPanel3.Children.Add(new ClickyInkOverlay(inkTextView3, clickyShapeView)
				{
					HorizontalAlignment = HorizontalAlignment.Center
				});
				string text2 = block.Source["note"];
				if (text2 != null)
				{
					InkTextView inkTextView4 = InkTextView.Plain(text2, ClickyPaperFonts.Hand(14.0), clickyInkStyle.Fresh);
					inkTextView4.HorizontalAlignment = HorizontalAlignment.Center;
					stackPanel3.Children.Add(inkTextView4);
				}
				clickyInkHStack.Children.Add(stackPanel3);
			}
			clickyInkHStack.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
			stackPanel.Children.Add(clickyInkHStack);
			Rectangle rectangle2 = Rule(clickyInkStyle.Ink.Opacity(0.4), 0.8);
			rectangle2.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
			stackPanel.Children.Add(rectangle2);
		}
		Grid grid2 = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
		double[] array3 = new double[5] { 415.0, 18.0, 0.8, 18.0, 415.0 };
		foreach (double pixels in array3)
		{
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(pixels)
			});
		}
		grid2.Margin = new Thickness(0.0, 0.0, -18.799999999999955, 0.0);
		FrameworkElement item = Column(source2.Where((ClickyInkGroup _, int i) => i % 2 == 0), clock, clickyInkStyle, paper2);
		FrameworkElement frameworkElement = Column(source2.Where((ClickyInkGroup _, int i) => i % 2 == 1), clock, clickyInkStyle, paper2);
		Rectangle rectangle3 = new Rectangle
		{
			Fill = Ui.Brush(clickyInkStyle.Ink.Opacity(0.25)),
			Width = 0.8
		};
		Grid.SetColumn(rectangle3, 2);
		Grid.SetColumn(frameworkElement, 4);
		grid2.Children.Add(item);
		grid2.Children.Add(rectangle3);
		grid2.Children.Add(frameworkElement);
		stackPanel.Children.Add(grid2);
		base.Children.Add(stackPanel);
	}

	private static Rectangle Rule(Color color, double height)
	{
		return new Rectangle
		{
			Fill = Ui.Brush(color),
			Height = height,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
	}

	private static FrameworkElement Column(IEnumerable<ClickyInkGroup> groups, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0,
			Width = 415.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		foreach (ClickyInkGroup group in groups)
		{
			stackPanel.Children.Add(ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions()).Column(378.0));
		}
		return stackPanel;
	}
}
