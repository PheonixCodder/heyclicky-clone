using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkDashboardFaceView
{
	public static readonly Size Size = new Size(330f, 210f);

	public static FrameworkElement Make(ClickyInkLiveNote note, double clock, ClickyHomeSpreadPaper? paper = null, int? runNumber = null)
	{
		ClickyHomeSpreadPaper clickyHomeSpreadPaper = paper ?? ClickyHomeSpreadPaper.Note;
		ClickyInkStyle clickyInkStyle = new ClickyInkStyle(clickyHomeSpreadPaper.Ink, clickyHomeSpreadPaper.Quiet, clickyHomeSpreadPaper.Deep, note.LatestEdit(clock));
		ClickyInkLiveNote clickyInkLiveNote = note.FoldingStruckWords(2.5);
		ClickyInkDashboardBoard clickyInkDashboardBoard = new ClickyInkDashboardBoard(clickyInkLiveNote, clock);
		ClickyInkLiveNote.Block run = clickyInkDashboardBoard.Run;
		ClickyInkRunLine clickyInkRunLine = ((run != null) ? new ClickyInkRunLine(run.Source) : null);
		Grid layout = new Grid
		{
			RowSpacing = 7.0,
			Padding = new Thickness(14.0)
		};
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
		ClickyInkLiveNote.Words title = clickyInkLiveNote.Title;
		if ((object)title != null)
		{
			clickyInkHStack.Children.Add(ClickyInkWordsView.Make(title, clock, clickyInkStyle, ClickyPaperFonts.Hand(21.0), pencil: false, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
				t.MinimumScale = 0.7;
			}));
		}
		clickyInkHStack.Children.Add(new ClickyInkSpacer());
		string text = clickyInkRunLine?.Number ?? runNumber?.ToString(CultureInfo.InvariantCulture);
		if (text != null)
		{
			UIElementCollection children = clickyInkHStack.Children;
			ClickyInkLiveNote.Block run2 = clickyInkDashboardBoard.Run;
			children.Add((run2 != null) ? ClickyInkDashboardFormNeeds.RunStampMark(text, run2.Source, clock, run2.LandsAt, 9.0) : new ClickyPaperStamp(ClickyThingStatus.Watching, "RUN " + text, 9.0, -5.0));
		}
		AddRow(clickyInkHStack);
		ClickyInkGroup quiet = clickyInkDashboardBoard.Quiet;
		if ((object)quiet != null)
		{
			AddRow(new ClickyInkHStack(7.0, InkVerticalAlignment.Center, ClickyInkDashboardFormNeeds.LampView(ClickyInkStatusState.Ok, 11.0, 2), ClickyInkWordsView.Make(quiet.Blocks[0].Words, clock, clickyInkStyle, ClickyPaperFonts.Hand(15.0), pencil: false, ClickyPaperInk.DoneGreen, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			})));
		}
		else
		{
			ClickyInkGroup answer = clickyInkDashboardBoard.Answer;
			if ((object)answer != null)
			{
				FrameworkElement frameworkElement = ClickyInkDashboardFormNeeds.LampView(clickyInkDashboardBoard.Overall, 11.0, 2);
				frameworkElement.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
				AddRow(new ClickyInkHStack(7.0, InkVerticalAlignment.Top, frameworkElement, ClickyInkWordsView.Make(answer.Blocks[0].Words, clock, clickyInkStyle, ClickyPaperFonts.Hand(15.0), pencil: false, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 2;
				})));
			}
		}
		if (clickyInkDashboardBoard.Strip.Count > 0)
		{
			Grid grid = new Grid
			{
				Padding = new Thickness(0.0, 5.0, 0.0, 5.0)
			};
			List<ClickyInkLiveNote.Block> list = clickyInkDashboardBoard.Strip.Take(3).ToList();
			for (int i = 0; i < list.Count; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				FrameworkElement frameworkElement2 = ClickyInkMiniTile.Make(list[i], clock, clickyInkStyle, i);
				frameworkElement2.VerticalAlignment = VerticalAlignment.Top;
				FrameworkElement frameworkElement3 = frameworkElement2;
				if (i > 0)
				{
					Border item = new Border
					{
						Width = 1.0,
						Background = Ui.Brush(ClickyInkDashboardFormNeeds.Pencil.Opacity(0.25)),
						Margin = new Thickness(0.0, 2.0, 0.0, 2.0),
						HorizontalAlignment = HorizontalAlignment.Left
					};
					frameworkElement3 = new Grid
					{
						Children = 
						{
							(UIElement)frameworkElement2,
							(UIElement)item
						}
					};
				}
				Grid.SetColumn(frameworkElement3, i);
				grid.Children.Add(frameworkElement3);
			}
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkRuledBox(3, 3.0), ClickyInkDashboardFormNeeds.Pencil.Opacity(0.45), 1.0);
			AddRow(new ClickyInkOverlay(grid, clickyShapeView));
		}
		AddRow(new Grid(), fills: true);
		ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(5.0, InkVerticalAlignment.Center);
		ClickyInkDashboardBoard.ChangeCounts changes = clickyInkDashboardBoard.Changes;
		if (changes.New > 0)
		{
			clickyInkHStack2.Children.Add(ClickyInkFormKit.Chip($"{changes.New} new", ClickyPaperInk.DoneGreen));
		}
		if (changes.Updated > 0)
		{
			clickyInkHStack2.Children.Add(ClickyInkFormKit.Chip($"{changes.Updated} updated", clickyInkStyle.Fresh));
		}
		if (changes.Gone > 0)
		{
			clickyInkHStack2.Children.Add(ClickyInkFormKit.Chip($"{changes.Gone} gone", ClickyInkDashboardFormNeeds.Down));
		}
		clickyInkHStack2.Children.Add(new ClickyInkSpacer());
		string text2 = clickyInkRunLine?.Next;
		if (text2 != null)
		{
			clickyInkHStack2.Children.Add(ClickyInkFormKit.Text("next " + text2, ClickyPaperFonts.Hand(13.0), clickyInkStyle.Quiet, 0.0, 1.0, 1));
		}
		AddRow(clickyInkHStack2);
		Grid obj = new Grid
		{
			Width = Size.Width,
			Height = Size.Height,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = 
			{
				(UIElement)new PaperSurface(ClickyShape.RoundedRectangle(10.0), clickyHomeSpreadPaper.Fill, 0.45).WithShadow(PaperColor.Black.Opacity(0.3), 12.0, 0.0, 5.0),
				(UIElement)layout
			}
		};
		ClickyInkFormKit.WithoutPenRenderer(obj);
		return obj;
		void AddRow(FrameworkElement e, bool fills = false)
		{
			layout.RowDefinitions.Add(new RowDefinition
			{
				Height = (fills ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto)
			});
			Grid.SetRow(e, layout.RowDefinitions.Count - 1);
			layout.Children.Add(e);
		}
	}
}
