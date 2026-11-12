using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackInkPulledCard : StackPanel
{
	public const double CardWidth = 420.0;

	public const double RoomAboveAndBelowWaiting = 236.0;

	public ClickyEdgeStackInkPulledCard(ClickyInkLiveNote note, ClickyEdgeStackInkReading reading, double clock, ClickyHomeSpreadPaper paper, bool isJob, string? liveStep, ClickyInkActions actions, Action? onOpen = null, Action? onPutBack = null, Action? onUnpin = null, double waitingMaximumHeight = 420.0)
	{
		base.Spacing = 12.0;
		base.Padding = new Thickness(0.0, 6.0, 0.0, 0.0);
		ClickyInkStyle clickyInkStyle = ClickyEdgeStackInkLook.Style(note, paper, clock, reading);
		ClickyInkGroup clickyInkGroup = WaitingGroup(note, reading, clock);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0
		};
		stackPanel.Children.Add(new ClickyEdgeStackInkHeader(reading, clock, clickyInkStyle, 27.0, 9.5));
		FrameworkElement frameworkElement = ClickyEdgeStackInkLead.Make(reading, clock, clickyInkStyle, isLarge: true, (object)clickyInkGroup == null);
		if ((object)frameworkElement != null)
		{
			stackPanel.Children.Add(frameworkElement);
		}
		FrameworkElement frameworkElement2 = ClickyEdgeStackInkFooter.Make(reading, liveStep, clickyInkStyle, 2, (object)clickyInkGroup != null);
		if ((object)frameworkElement2 != null)
		{
			stackPanel.Children.Add(frameworkElement2);
		}
		if ((object)clickyInkGroup != null)
		{
			FrameworkElement frameworkElement3 = Waiting(clickyInkGroup, clock, clickyInkStyle, paper, actions, waitingMaximumHeight);
			frameworkElement3.Margin = new Thickness(-6.0, -2.0, -6.0, -6.0);
			stackPanel.Children.Add(frameworkElement3);
		}
		List<string> list = OtherPageNames(note, clock);
		if (list.Count > 0)
		{
			TextBlock textBlock = PaperText.Line("also in the note: " + string.Join(" · ", list), ClickyPaperFonts.Hand(14.5), clickyInkStyle.Quiet);
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock.MaxLines = 1;
			textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			stackPanel.Children.Add(textBlock);
		}
		Grid grid = new Grid
		{
			Width = 420.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		grid.Children.Add(new ClickyEdgeStackInkPaper(paper, isJob, isLifted: true));
		stackPanel.Padding = new Thickness(21.0, isJob ? 22 : 20, 21.0, 18.0);
		grid.Children.Add(stackPanel);
		base.Children.Add(grid);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stackPanel2.Children.Add(new ClickyPaperGelButton("Open", onOpen, ClickyPaperGel.Blue, 32.0, 13.0, 13.0));
		stackPanel2.Children.Add(new ClickyPaperGelButton("Put back", onPutBack, ClickyPaperGel.White, 32.0, 13.0, 13.0));
		stackPanel2.Children.Add(new ClickyPaperGelButton("Unpin", onUnpin, ClickyPaperGel.White, 32.0, 13.0, 13.0));
		base.Children.Add(stackPanel2);
	}

	private static List<string> OtherPageNames(ClickyInkLiveNote note, double clock)
	{
		return (from page in note.Pages.Skip(1)
			where page.LandsAt <= clock && page.Blocks.Any((ClickyInkLiveNote.Block b) => b.IsPresent(clock))
			select ClickyInkParser.Emphasis.Strip(page.Name.TextAt(clock)).TrimWs() into name
			where name.Length > 0
			select name).ToList();
	}

	private static ClickyInkGroup? WaitingGroup(ClickyInkLiveNote note, ClickyEdgeStackInkReading reading, double clock)
	{
		string id = reading.WaitingBlockID ?? reading.PendingBlockID;
		if (id == null)
		{
			return null;
		}
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			ClickyInkGroup clickyInkGroup = ClickyInkGroup.Groups(page, clock).FirstOrDefault((ClickyInkGroup g) => g.Blocks.Any((ClickyInkLiveNote.Block b) => b.Id == id));
			if ((object)clickyInkGroup != null)
			{
				return clickyInkGroup;
			}
		}
		return null;
	}

	private static FrameworkElement Waiting(ClickyInkGroup group, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper, ClickyInkActions actions, double maximumHeight)
	{
		FrameworkElement e;
		using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default with
		{
			ContentWidth = 378.0
		}))
		{
			e = ClickyInkGroupView.Make(group, clock, style, paper, actions);
		}
		FrameworkElement frameworkElement = e.Column(378.0);
		frameworkElement.Margin = new Thickness(6.0);
		return new ClickyEdgeStackFitOrScroll(maximumHeight, frameworkElement)
		{
			Margin = new Thickness(-6.0)
		};
	}
}
