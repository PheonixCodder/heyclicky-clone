using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkDeskView : Grid
{
	private static readonly double[] Tilts = new double[6] { -2.2, 1.4, -0.8, 2.0, -1.6, 0.9 };

	public static double CardWidth => 410.0;

	public ClickyInkDeskView(ClickyInkLiveNote note, double clock, double width = 1440.0, ClickyHomeSpreadPaper? paper = null)
	{
		ClickyHomeSpreadPaper paper2 = paper ?? ClickyHomeSpreadPaper.Note;
		ClickyInkStyle style = new ClickyInkStyle(paper2.Ink, paper2.Quiet, paper2.Deep, note.LatestEdit(clock));
		List<(string, ClickyInkGroup)> source = note.Pages.SelectMany((ClickyInkLiveNote.Page page) => from g in ClickyInkGroup.Groups(page, clock)
			select (Page: page.Name.Current, Group: g)).ToList();
		(string Page, ClickyInkGroup Group) answer = source.FirstOrDefault<(string, ClickyInkGroup)>(((string Page, ClickyInkGroup Group) g) => g.Group.Kind == ClickyInkDocument.Kind.Answer);
		List<(string, ClickyInkGroup)> source2 = source.Where<(string, ClickyInkGroup)>(((string Page, ClickyInkGroup Group) g) => g.Group.Id != answer.Group?.Id).ToList();
		int columns = Math.Max(1, (int)((width - 40.0) / (CardWidth + 36.0)));
		base.Width = width;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 36.0,
			Padding = new Thickness(30.0, 34.0, 30.0, 34.0)
		};
		int column;
		for (column = 0; column < columns; column++)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 26.0,
				Width = CardWidth,
				VerticalAlignment = VerticalAlignment.Top
			};
			if (column == 0)
			{
				stackPanel2.Children.Add(TitleCard(note, answer.Group, clock, style, paper2));
			}
			foreach (var (label, group) in source2.Where<(string, ClickyInkGroup)>(((string Page, ClickyInkGroup Group) _, int i) => (i + 1) % columns == column))
			{
				stackPanel2.Children.Add(Card(group, label, clock, style, paper2));
			}
			stackPanel.Children.Add(stackPanel2);
		}
		base.Children.Add(stackPanel);
	}

	private static FrameworkElement TitleCard(ClickyInkLiveNote note, ClickyInkGroup? answer, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(16.0)
		};
		ClickyInkLiveNote.Words title = note.Title;
		if ((object)title != null)
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(title, clock, style, ClickyPaperFonts.Hand(30.0), pencil: true, null, null));
		}
		if ((object)answer != null)
		{
			stackPanel.Children.Add(ClickyInkGroupView.Make(answer, clock, style, paper, new ClickyInkActions()));
		}
		return new Grid
		{
			Width = CardWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = 
			{
				(UIElement)PaperCard(PaperColor.Hex("#FFFDF6")),
				(UIElement)stackPanel
			}
		}.Transformed(0.0, 0.0, -1.0, 1.0, null);
	}

	private static FrameworkElement Card(ClickyInkGroup group, string label, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper)
	{
		ClickyInkLiveNote.Block block = group.Blocks[0];
		double num = ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.55);
		double degrees = Tilts[(uint)(ClickyPaperRandom.StableSeed(block.Id) % 6)];
		ClickyInkDocument.Kind kind = group.Kind;
		FrameworkElement frameworkElement;
		if (((uint)(kind - 11) <= 3u || (uint)(kind - 17) <= 1u) ? true : false)
		{
			frameworkElement = ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions()).Column(CardWidth);
		}
		else
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 8.0,
				Padding = new Thickness(16.0)
			};
			stackPanel.Children.Add(InkTextView.Plain(label.ToLowerInvariant(), ClickyPaperFonts.Hand(15.0), PaperColor.Hex("#C2410C").Opacity(0.8)));
			stackPanel.Children.Add(ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions()).Column(378.0));
			frameworkElement = new Grid
			{
				Width = CardWidth,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children = 
				{
					(UIElement)PaperCard(PaperColor.Hex("#FBF8EF"), ruled: true),
					(UIElement)stackPanel
				}
			};
		}
		double num2 = 1.0 - EaseOut(num);
		frameworkElement.Transformed(0.0, 0.0, degrees, 1.0, null);
		frameworkElement.Transformed(0.0, -40.0 * num2, 0.0, 1.0 + 0.12 * num2, null);
		frameworkElement.Opacity = Math.Min(1.0, num * 2.5);
		return frameworkElement;
	}

	private static FrameworkElement PaperCard(Color tint, bool ruled = false)
	{
		PaperSurface paperSurface = new PaperSurface(ClickyShape.RoundedRectangle(4.0), PaperFill.Of(tint), 0.4).WithShadow(PaperColor.Black.Opacity(0.35), 7.0, 0.0, 5.0);
		if (ruled)
		{
			paperSurface.WithWash(PaperFill.Of(PaperColor.Hex("#E5484D").Opacity(0.35)), (Size s) => new Rect(0.0, 34.0, s.Width, 1.0));
			paperSurface.Washes.Add(new PaperWash(PaperFill.Of(PaperColor.Hex("#3C5AA0").Opacity(0.09)), null, Clipped: true, delegate(Size size)
			{
				PaperPath paperPath = new PaperPath();
				for (double num = 57.0; num < size.Height; num += 23.0)
				{
					paperPath.AddRect(new Rect(0.0, num, size.Width, 1.0));
				}
				return paperPath;
			}));
		}
		return paperSurface;
	}

	private static double EaseOut(double t)
	{
		return 1.0 - Math.Pow(1.0 - t, 3.0);
	}
}
