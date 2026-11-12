using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkFrontPagePlans
{
	private sealed class Measurer(double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper) : IClickyInkFrontPageMeasurer
	{
		public double ContentWidth => 378.0;

		public double Group(ClickyInkGroup group)
		{
			return ClickyInkFormKit.MeasureHeight(ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions()), 378.0);
		}

		public double Table(ClickyInkGroup group, double width)
		{
			return ClickyInkFormKit.MeasureHeight(ClickyInkTable.Create(group.Blocks[0], clock, style, width), width);
		}

		public double Tiles(IReadOnlyList<ClickyInkLiveNote.Block> blocks, double width)
		{
			return ClickyInkFormKit.MeasureHeight(ClickyInkDashboardFormNeeds.Tiles(blocks.ToList(), clock, style, width), width);
		}

		public double MissionTileHeight(IReadOnlyList<ClickyInkDocument.Block> blocks)
		{
			return ClickyInkFrontPagePlans.MissionTileHeight(blocks);
		}

		public double Headline(ClickyInkDashboardBoard board, double width, bool isMission)
		{
			double width2 = width - (double)(isMission ? 36 : 0);
			ClickyInkGroup quiet = board.Quiet;
			FrameworkElement frameworkElement;
			if ((object)quiet != null)
			{
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 4.0
				};
				stackPanel.Children.Add(ClickyInkWordsView.Make(quiet.Blocks[0].Words, clock, style, ClickyPaperFonts.Hand(isMission ? 34 : 42), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 2;
				}));
				stackPanel.Children.Add(ClickyInkFormKit.Text("checked", ClickyPaperFonts.Typed(14.0, FontWeights.Medium), style.Ink, 0.0, 1.0, 1));
				ClickyInkLiveNote.Block block = board.Answer?.Blocks.FirstOrDefault();
				if (block != null)
				{
					stackPanel.Children.Add(ClickyInkFormKit.Text("last news " + block.Words.Current, ClickyPaperFonts.Hand(21.0), style.Ink, 0.0, 1.0, 2).Padding(0.0, 4.0, 0.0, 0.0));
				}
				frameworkElement = new ClickyInkHStack(22.0, InkVerticalAlignment.Center, new ClickyPaperStamp(ClickyThingStatus.Ready, "ALL QUIET", 22.0), ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer())
				{
					Width = width2
				};
			}
			else
			{
				ClickyInkLiveNote.Block block2 = board.Answer?.Blocks.FirstOrDefault();
				if (block2 != null)
				{
					int num = block2.Words.Current.CharacterCount();
					double size = ((!isMission) ? ((num > 80) ? 36 : ((num > 50) ? 42 : 48)) : ((num > 70) ? 30 : 36));
					ClickyInkHStack clickyInkHStack = new ClickyInkHStack(22.0, InkVerticalAlignment.Center)
					{
						Width = width2
					};
					if (isMission)
					{
						clickyInkHStack.Children.Add(new Grid
						{
							Width = 28.0,
							Height = 28.0
						});
					}
					clickyInkHStack.Children.Add(ClickyInkWordsView.Make(block2.Words, clock, style, ClickyPaperFonts.Hand(size), pencil: true, null, null).With(delegate(InkTextView t)
					{
						t.LineSpacing = -2.0;
					}));
					clickyInkHStack.Children.Add(new ClickyInkSpacer());
					string text = block2.Source["big"];
					if (text != null && text.Length > 0)
					{
						StackPanel stackPanel2 = new StackPanel
						{
							Spacing = 2.0
						};
						stackPanel2.Children.Add(ClickyInkFormKit.Text(text, ClickyPaperFonts.BigNumber((text.CharacterCount() > 6) ? 46 : 60), style.Ink, 0.0, 1.0, 1).Padding(12.0, 4.0, 12.0, 4.0));
						string text2 = block2.Source["note"];
						if (text2 != null && text2.Length > 0)
						{
							stackPanel2.Children.Add(ClickyInkFormKit.Text(text2, ClickyPaperFonts.Hand(19.0), style.Ink, 0.0, 1.0, 2).With(delegate(InkTextView t)
							{
								t.TextAlignment = TextAlignment.Center;
							}).MaxWidthOf(220.0));
						}
						clickyInkHStack.Children.Add(stackPanel2);
					}
					frameworkElement = clickyInkHStack;
				}
				else
				{
					frameworkElement = new Grid
					{
						Height = 10.0
					};
				}
			}
			frameworkElement.Margin = new Thickness(isMission ? 18 : 0);
			return ClickyInkFormKit.MeasureHeight(frameworkElement, width);
		}
	}

	private static readonly Dictionary<string, ClickyInkFrontPagePlanner.Plan> Cache = new Dictionary<string, ClickyInkFrontPagePlanner.Plan>();

	public static double MissionTileHeight(IReadOnlyList<ClickyInkDocument.Block> blocks)
	{
		return ((blocks.Count == 0) ? 90.0 : ((IEnumerable<ClickyInkDocument.Block>)blocks).Max((Func<ClickyInkDocument.Block, double>)ClickyInkDashboardFormNeeds.TileHeight)) + 4.0;
	}

	public static ClickyInkFrontPagePlanner.Plan For(ClickyInkLiveNote note, ClickyInkFrontPageVariant variant, ClickyHomeSpreadPaper paper)
	{
		string key = ClickyInkFrontPagePlanner.Key(note, variant);
		if (Cache.TryGetValue(key, out ClickyInkFrontPagePlanner.Plan value))
		{
			return value;
		}
		double clock = note.FinishedClock + 30.0;
		ClickyInkStyle style = new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock));
		ClickyInkFrontPagePlanner.Plan plan = ClickyInkFrontPagePlanner.Make(note, variant, new Measurer(clock, style, paper));
		if (Cache.Count > 60)
		{
			Cache.Clear();
		}
		Cache[key] = plan;
		return plan;
	}
}
