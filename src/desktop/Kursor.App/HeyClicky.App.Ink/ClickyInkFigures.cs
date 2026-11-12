using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkFigures
{
	private static readonly Color DownOrange = PaperColor.Hex("#C2410C");

	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		Grid grid = new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			ColumnSpacing = 18.0
		};
		foreach (ClickyInkLiveNote.Block item in blocks.Take(3))
		{
			if (clock < item.LandsAt)
			{
				continue;
			}
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 2.0,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			string text = item.Source["_was"];
			InkTextView inkTextView = InkTextView.Plain(item.Source["value"] ?? item.Source["big"] ?? "", ClickyPaperFonts.BigNumber(30.0), (text != null) ? style.Fresh : style.Ink).With(delegate(InkTextView t)
			{
				t.Tracking = -1.0;
				t.LineLimit = 1;
				t.MinimumScale = 0.5;
			});
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.FirstBaseline, inkTextView);
			if (text != null)
			{
				clickyInkHStack.Children.Add(InkTextView.Plain(text, ClickyPaperFonts.BigNumber(15.0), style.Quiet).With(delegate(InkTextView t)
				{
					t.Strikethrough = style.Fresh;
				}));
			}
			stackPanel.Children.Add(clickyInkHStack);
			stackPanel.Children.Add(ClickyInkWordsView.Make(item.Words, clock, style, ClickyPaperFonts.Hand(15.0), pencil: false, style.Quiet, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
			ClickyInkFigureRules.Delta delta = ClickyInkFigureRules.Read(item.Source["delta"]);
			if ((object)delta != null)
			{
				stackPanel.Children.Add(delta.IsCaption ? InkTextView.Plain(delta.Text, ClickyPaperFonts.Typed(11.0, FontWeights.Medium), style.Quiet) : InkTextView.Plain(delta.Text, ClickyPaperFonts.Typed(11.0, FontWeights.SemiBold), delta.IsDown ? DownOrange : ClickyPaperInk.DoneGreen));
			}
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			Grid.SetColumn(stackPanel, grid.Children.Count);
			grid.Children.Add(stackPanel);
		}
		return grid;
	}
}
