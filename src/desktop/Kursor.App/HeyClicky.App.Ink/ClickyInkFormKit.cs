using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkFormKit
{
	public static InkTextView Text(string text, PaperFont font, Color color, double tracking = 0.0, double minimumScale = 1.0, int? lineLimit = 1)
	{
		return InkTextView.Plain(text, font, color).With(delegate(InkTextView t)
		{
			t.Tracking = tracking;
			t.MinimumScale = minimumScale;
			t.LineLimit = lineLimit;
		});
	}

	public static FrameworkElement MaxWidthOf(this FrameworkElement e, double width)
	{
		e.MaxWidth = width;
		return e;
	}

	public static FrameworkElement Pill(string words, PaperFont font, Color color, Color stroke, double padding, double lineWidth = 1.0, double degrees = -1.5)
	{
		InkTextView inkTextView = Text(words, font, color, 0.0, 1.0, 1);
		inkTextView.Margin = new Thickness(padding, 0.0, padding, 0.0);
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(ClickyShape.Capsule, stroke, lineWidth);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(inkTextView, clickyShapeView)
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
		if (degrees != 0.0)
		{
			clickyInkOverlay.Transformed(0.0, 0.0, degrees, 1.0, null);
		}
		return clickyInkOverlay;
	}

	public static FrameworkElement Chip(string words, Color color)
	{
		return Pill(words, ClickyPaperFonts.Hand(12.5), color, color.Opacity(0.45), 4.0, 0.8, 0.0);
	}

	public static FrameworkElement? Changes(ClickyInkDashboardBoard.ChangeCounts changes, ClickyInkStyle style, double size, double padding)
	{
		if (changes.Total <= 0)
		{
			return null;
		}
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		if (changes.New > 0)
		{
			stackPanel.Children.Add(Piece($"{changes.New} new", ClickyPaperInk.DoneGreen));
		}
		if (changes.Updated > 0)
		{
			stackPanel.Children.Add(Piece($"{changes.Updated} updated", style.Fresh));
		}
		if (changes.Gone > 0)
		{
			stackPanel.Children.Add(Piece($"{changes.Gone} gone", ClickyInkDashboardFormNeeds.Down));
		}
		return stackPanel;
		FrameworkElement Piece(string words, Color color)
		{
			return Pill(words, ClickyPaperFonts.Hand(size), color, color.Opacity(0.45), padding);
		}
	}

	public static FrameworkElement SourceLink(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, double arrowSize, double wordsSize)
	{
		bool flag = block.IsStruck(clock);
		InkTextView inkTextView = new InkTextView(ClickyInkWords.Text(block.Words, clock, style, pencil: false, flag ? style.Quiet : style.Ink.Opacity(0.85), null), ClickyPaperFonts.Typed(wordsSize, FontWeights.Medium), style.Ink)
		{
			LineLimit = 1
		};
		if (flag)
		{
			inkTextView.Strikethrough = style.Fresh;
		}
		ClickyInkHStack item = new ClickyInkHStack(4.0, InkVerticalAlignment.FirstBaseline, Text("↗", ClickyPaperFonts.Typed(arrowSize, FontWeights.Bold), style.Fresh, 0.0, 1.0, 1), inkTextView);
		Pressable pressable = new Pressable();
		pressable.Children.Add(item);
		pressable.Click += delegate
		{
			string text = block.Source["url"];
			if (text != null)
			{
				actions.Open(text);
			}
		};
		return pressable;
	}

	public static FrameworkElement Squiggle(int seed, Color color)
	{
		return ClickyInkHStack.Flexible(new ClickyInkFill(ClickyShapeView.Stroke(new ClickyInkSquiggle(seed), new PaperStroke(color, 1.2)), 6.0));
	}

	public static PaperWash Dots(double spacing, double radius)
	{
		return new PaperWash(PaperFill.Of(PaperColor.Hex("#3C5AA0").Opacity(0.16)), null, Clipped: true, delegate(Size size)
		{
			PaperPath paperPath = new PaperPath();
			for (double num = spacing; num < size.Height; num += spacing)
			{
				for (double num2 = spacing; num2 < size.Width; num2 += spacing)
				{
					paperPath.AddEllipse(new Rect(num2 - radius, num - radius, radius * 2.0, radius * 2.0));
				}
			}
			return paperPath;
		});
	}

	public static double MeasureHeight(FrameworkElement view, double width)
	{
		Grid grid = new Grid();
		grid.Width = width;
		grid.Children.Add(view);
		grid.Measure(new Size(width, double.PositiveInfinity));
		return grid.DesiredSize.Height;
	}

	public static void WithoutPenRenderer(UIElement root)
	{
		if (!(root is InkTextView inkTextView))
		{
			if (!(root is Panel panel))
			{
				if (root is Border { Child: var child })
				{
					if ((object)child != null)
					{
						WithoutPenRenderer(child);
					}
				}
				else if (root is ContentControl { Content: UIElement content })
				{
					WithoutPenRenderer(content);
				}
				return;
			}
			{
				foreach (UIElement child2 in panel.Children)
				{
					WithoutPenRenderer(child2);
				}
				return;
			}
		}
		ClickyInkTextContent content2 = inkTextView.Content;
		if (content2.PenRun >= 0 || content2.Runs.Any((ClickyInkRun r) => r.IsLink))
		{
			ClickyInkTextContent content3 = new ClickyInkTextContent(content2.Runs.Select((ClickyInkRun r) => r with
			{
				IsLink = false
			}));
			inkTextView.Content = content3;
		}
	}
}
