using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkDashboardView
{
	public sealed record Job(string CapsLine, ClickyThingStatus Status, string? StampWord = null, string? LiveLine = null);

	private sealed class Builder
	{
		private readonly ClickyInkLiveNote note = rawNote.FoldingStruckWords(2.5);

		private ClickyInkStyle? style;

		private ClickyInkStyle Style => style ?? (style = new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock)));

		private double InnerWidth => width - 72.0;

		private bool IsTwoColumns => width >= 760.0;

		public Builder(ClickyInkLiveNote rawNote, double clock, ClickyHomeSpreadPaper paper, ClickyInkActions actions, double width, int? runNumber, string? dateLine, Job? job, ClickyInkTiles.Look tilesLook)
		{
		}

		public FrameworkElement Body()
		{
			ClickyInkDashboardBoard clickyInkDashboardBoard = new ClickyInkDashboardBoard(note, clock, ((object)job == null) ? "This run" : "In short");
			List<ClickyInkDashboardSplit.Placed> list = (IsTwoColumns ? ClickyInkDashboardRows.For(note, paper).Placing(clickyInkDashboardBoard.Sections) : new List<ClickyInkDashboardSplit.Placed>());
			StackPanel stackPanel = new StackPanel
			{
				Width = InnerWidth,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			stackPanel.Children.Add(Masthead(clickyInkDashboardBoard).Padding(0.0, 0.0, 0.0, 12.0));
			FrameworkElement frameworkElement = Headline(clickyInkDashboardBoard);
			if ((object)frameworkElement != null)
			{
				stackPanel.Children.Add(frameworkElement);
			}
			if (clickyInkDashboardBoard.Strip.Count > 0)
			{
				stackPanel.Children.Add(ClickyInkDashboardFormNeeds.Tiles(clickyInkDashboardBoard.Strip, clock, Style, InnerWidth, tilesLook).Padding(0.0, 14.0, 0.0, 8.0));
			}
			if (IsTwoColumns)
			{
				for (int i = 0; i < list.Count; i++)
				{
					ClickyInkDashboardSplit.Placed placed = list[i];
					if (!(placed is ClickyInkDashboardSplit.Placed.Columns columns))
					{
						if (placed is ClickyInkDashboardSplit.Placed.Wide wide)
						{
							stackPanel.Children.Add(WideSection(wide.Section).Padding(0.0, 16.0, 0.0, 0.0));
						}
						continue;
					}
					if (columns.Left.Count + columns.Right.Count == 1 && !columns.HasMove)
					{
						stackPanel.Children.Add(WideSection(columns.Left.Concat(columns.Right).First()).Padding(0.0, 16.0, 0.0, 0.0));
						continue;
					}
					ClickyInkDashboardSplit.Placed.Columns columns2 = columns;
					ClickyInkDashboardBoard.Section section = (columns2.HasMove ? clickyInkDashboardBoard.Move : null);
					ClickyInkDashboardBoard.Section move = (((object)section == null) ? null : section with
					{
						Groups = section.Groups.Take(columns2.MoveOnRight).ToList()
					});
					ClickyInkDashboardBoard.Section moreMove = (((object)section != null && section.Groups.Count > columns2.MoveOnRight) ? new ClickyInkDashboardBoard.Section("move-more", ClickyInkLiveNote.Words.Plain("Your move"), section.Groups.Skip(columns2.MoveOnRight).ToList()) : null);
					ClickyInkHStack clickyInkHStack = new ClickyInkHStack(36.0, InkVerticalAlignment.Top, Column(columns2.Left, null, moreMove), Column(columns2.Right, move));
					FrameworkElement frameworkElement2 = clickyInkHStack;
					if (columns2.Left.Count > 0 && (columns2.Right.Count > 0 || columns2.HasMove))
					{
						ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkRuledLine(new Point(0.5, 0.02), new Point(0.5, 0.98), 9 + i), PaperStroke.Round(paper.Ink.Opacity(0.14), 1.0));
						clickyShapeView.Width = 1.0;
						clickyShapeView.HorizontalAlignment = HorizontalAlignment.Center;
						frameworkElement2 = new ClickyInkOverlay(clickyInkHStack, clickyShapeView);
					}
					frameworkElement2.HorizontalAlignment = HorizontalAlignment.Left;
					stackPanel.Children.Add(frameworkElement2.Padding(0.0, 16.0, 0.0, 0.0));
				}
			}
			else
			{
				stackPanel.Children.Add(Column(clickyInkDashboardBoard.Sections, clickyInkDashboardBoard.Move).Padding(0.0, 14.0, 0.0, 0.0));
			}
			if (clickyInkDashboardBoard.Sources.Count > 0)
			{
				stackPanel.Children.Add(Footer(clickyInkDashboardBoard).Padding(0.0, 14.0, 0.0, 0.0));
			}
			stackPanel.Margin = new Thickness(36.0, 30.0, 36.0, 28.0);
			return new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Children = 
				{
					(UIElement)Sheet(),
					(UIElement)stackPanel,
					(UIElement)Tape(-38.0, -16.0, HorizontalAlignment.Left),
					(UIElement)Tape(38.0, 16.0, HorizontalAlignment.Right)
				}
			};
		}

		private FrameworkElement Masthead(ClickyInkDashboardBoard board)
		{
			ClickyInkLiveNote.Block run = board.Run;
			ClickyInkRunLine clickyInkRunLine = ((run != null) ? new ClickyInkRunLine(run.Source) : null);
			string text = clickyInkRunLine?.Number ?? runNumber?.ToString(CultureInfo.InvariantCulture);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 2.0
			};
			ClickyInkLiveNote.Words title = note.Title;
			if ((object)title != null)
			{
				stackPanel.Children.Add(ClickyInkWordsView.Make(title, clock, Style, ClickyPaperFonts.Hand(34.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.6;
				}));
			}
			string text3;
			if ((object)job == null)
			{
				string[] obj = new string[2] { "KEPT BY CLICKY", null };
				string text2 = clickyInkRunLine?.Next;
				obj[1] = ((text2 != null) ? ("NEXT RUN " + text2).ToUpperInvariant() : null);
				text3 = string.Join("  ·  ", obj.Where((string s) => s != null));
			}
			else
			{
				text3 = job.CapsLine.ToUpperInvariant();
			}
			string text4 = text3;
			stackPanel.Children.Add(ClickyInkFormKit.Text(text4, ClickyPaperFonts.Typed(9.5, FontWeights.Bold), Style.Quiet, 1.3, 0.7, 1));
			StackPanel right = new StackPanel
			{
				Spacing = 3.0,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			if ((object)job != null)
			{
				AddRight(new ClickyPaperStamp(job.Status, job.StampWord, 12.5, -5.0));
				string liveLine = job.LiveLine;
				if (liveLine != null)
				{
					AddRight(ClickyInkFormKit.Text(liveLine, ClickyPaperFonts.Hand(15.0), ClickyPaperInk.ClickyBlue, 0.0, 1.0, 1).With(delegate(InkTextView t)
					{
						t.TextAlignment = TextAlignment.Right;
					}).MaxWidthOf(300.0));
				}
			}
			else
			{
				ClickyInkLiveNote.Block run2 = board.Run;
				if (run2 != null)
				{
					AddRight(ClickyInkDashboardFormNeeds.RunStampMark(text, run2.Source, clock, run2.LandsAt, 12.5));
				}
				else if (text != null)
				{
					AddRight(new ClickyPaperStamp(ClickyThingStatus.Watching, "RUN " + text, 12.5, -5.0));
				}
			}
			string text5 = clickyInkRunLine?.RanAt ?? dateLine;
			if (text5 != null)
			{
				AddRight(ClickyInkFormKit.Text(text5, ClickyPaperFonts.Typed(11.5, FontWeights.SemiBold), Style.Ink.Opacity(0.7), 0.0, 1.0, 1));
			}
			string text6 = clickyInkRunLine?.Extra;
			if (text6 != null)
			{
				AddRight(ClickyInkFormKit.Text(text6, ClickyPaperFonts.Hand(15.0), Style.Quiet, 0.0, 1.0, 1));
			}
			FrameworkElement frameworkElement = Changes(board.Changes, 15.0, 5.0);
			if ((object)frameworkElement != null)
			{
				AddRight(frameworkElement.Padding(0.0, 2.0, 0.0, 0.0));
			}
			return new ClickyInkHStack(16.0, InkVerticalAlignment.Top, ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer(), right);
			void AddRight(FrameworkElement e)
			{
				e.HorizontalAlignment = HorizontalAlignment.Right;
				right.Children.Add(e);
			}
		}

		private FrameworkElement? Changes(ClickyInkDashboardBoard.ChangeCounts changes, double size, double padding)
		{
			return ClickyInkFormKit.Changes(changes, Style, size, padding);
		}

		private FrameworkElement? Headline(ClickyInkDashboardBoard board)
		{
			ClickyInkGroup quiet = board.Quiet;
			if ((object)quiet != null)
			{
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 8.0,
					Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
				};
				stackPanel.Children.Add(ClickyInkDashboardFormNeeds.Quiet(quiet.Blocks[0], clock, Style, InnerWidth, isHeadline: true));
				ClickyInkGroup answer = board.Answer;
				if ((object)answer != null)
				{
					stackPanel.Children.Add(LastNews(answer.Blocks[0]));
				}
				return ClickyInkGrow.Around(stackPanel, clock, quiet.LandsAt, quiet.VanishesAt);
			}
			ClickyInkGroup answer2 = board.Answer;
			if ((object)answer2 != null)
			{
				ClickyInkLiveNote.Block block = answer2.Blocks[0];
				double? num = ClickyInkChange.ChangedAt(block.Source);
				double num2;
				if (num.HasValue)
				{
					double valueOrDefault = num.GetValueOrDefault();
					num2 = block.Words.FinishOfWriting(valueOrDefault);
				}
				else
				{
					num2 = block.Words.FinishedAt;
				}
				double num3 = num2;
				ClickyInkHStack clickyInkHStack = new ClickyInkHStack(14.0, InkVerticalAlignment.Center);
				clickyInkHStack.Children.Add(ClickyInkDashboardFormNeeds.LampView(board.Overall, 20.0, 4, ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.5)));
				bool flag = block.IsStruck(clock);
				InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, Style, ClickyPaperFonts.Hand((block.Words.Current.CharacterCount() > 70) ? 22 : 26), pencil: true, flag ? new Color?(Style.Quiet) : ((Color?)null), null);
				if (flag)
				{
					inkTextView.Strikethrough = Style.Fresh;
				}
				clickyInkHStack.Children.Add(inkTextView);
				clickyInkHStack.Children.Add(new ClickyInkSpacer());
				string text = block.Source["big"];
				if (text != null && text.Length > 0)
				{
					FrameworkElement frameworkElement = ClickyInkDashboardFormNeeds.TickingNumber(block.Source, "big", clock, Style, (text.CharacterCount() > 6) ? 30 : 38, Math.Max(0.0, num3 - (num ?? num3)), text, ClickyInkChange.Previous(block.Source, "big") ?? block.Source["_was"], showsWas: false);
					frameworkElement.Margin = new Thickness(8.0, 2.0, 8.0, 2.0);
					ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(text.CharacterCount()).Trim(0.0, ClickyInkClock.Progress(clock, num3, num3 + 0.5)), PaperStroke.Round(Style.Fresh.Opacity(0.8), 2.2));
					clickyShapeView.Margin = new Thickness(-4.0);
					StackPanel stackPanel2 = new StackPanel
					{
						HorizontalAlignment = HorizontalAlignment.Right
					};
					ClickyInkOverlay item = new ClickyInkOverlay(frameworkElement, clickyShapeView)
					{
						HorizontalAlignment = HorizontalAlignment.Right
					};
					stackPanel2.Children.Add(item);
					string text2 = block.Source["note"];
					if (text2 != null && text2.Length > 0)
					{
						InkTextView inkTextView2 = ClickyInkFormKit.Text((num.HasValue && clock < num3) ? (ClickyInkChange.Previous(block.Source, "note") ?? text2) : text2, ClickyPaperFonts.Hand(15.0), Style.Fresh, 0.0, 1.0, 1);
						inkTextView2.HorizontalAlignment = HorizontalAlignment.Right;
						stackPanel2.Children.Add(inkTextView2);
					}
					clickyInkHStack.Children.Add(stackPanel2);
				}
				clickyInkHStack.Margin = new Thickness(0.0, 4.0, 0.0, 4.0);
				return ClickyInkGrow.Around(clickyInkHStack, clock, answer2.LandsAt, answer2.VanishesAt);
			}
			return null;
		}

		private FrameworkElement LastNews(ClickyInkLiveNote.Block block)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
			clickyInkHStack.Children.Add(ClickyInkFormKit.Pill("last news", ClickyPaperFonts.Hand(15.0), Style.Quiet, Style.Quiet.Opacity(0.5), 5.0));
			clickyInkHStack.Children.Add(ClickyInkWordsView.Make(block.Words, clock, Style, ClickyPaperFonts.Hand(18.0), pencil: false, Style.Ink.Opacity(0.75), null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
			string text = block.Source["big"];
			if (text != null && text.Length > 0)
			{
				clickyInkHStack.Children.Add(ClickyInkFormKit.Text(text, ClickyPaperFonts.BigNumber(15.0), Style.Ink.Opacity(0.7), 0.0, 1.0, 1));
			}
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			clickyInkHStack.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
			return clickyInkHStack;
		}

		private FrameworkElement Column(IEnumerable<ClickyInkDashboardBoard.Section> sections, ClickyInkDashboardBoard.Section? move, ClickyInkDashboardBoard.Section? moreMove = null)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 18.0,
				Width = (IsTwoColumns ? 378.0 : InnerWidth),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			if ((object)move != null)
			{
				stackPanel.Children.Add(Section(move, isMove: true));
			}
			foreach (ClickyInkDashboardBoard.Section section in sections)
			{
				stackPanel.Children.Add(Section(section, isMove: false));
			}
			if ((object)moreMove != null)
			{
				stackPanel.Children.Add(Section(moreMove, isMove: true, showsHeader: false));
			}
			return stackPanel;
		}

		private FrameworkElement Header(ClickyInkDashboardBoard.Section section, bool isMove)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
			if (isMove)
			{
				clickyInkHStack.Children.Add(ClickyInkFormKit.Text("your move", ClickyPaperFonts.Hand(21.0), PaperColor.Hex("#B05F00"), 0.0, 1.0, 1));
			}
			else
			{
				clickyInkHStack.Children.Add(ClickyInkWordsView.Make(section.Name, clock, Style, ClickyPaperFonts.Hand(21.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
			int num = section.NewCount(clock);
			if (num > 0 && !section.IsNamedNew)
			{
				clickyInkHStack.Children.Add(ClickyInkTag.Make($"{num} new", ClickyPaperInk.DoneGreen));
			}
			clickyInkHStack.Children.Add(ClickyInkFormKit.Squiggle(section.Id.CharacterCount(), Style.Ink.Opacity(0.16)));
			clickyInkHStack.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
			return clickyInkHStack;
		}

		private FrameworkElement Section(ClickyInkDashboardBoard.Section section, bool isMove, bool showsHeader = true)
		{
			StackPanel stackPanel = new StackPanel();
			if (showsHeader)
			{
				stackPanel.Children.Add(Header(section, isMove));
			}
			foreach (ClickyInkGroup group in section.Groups)
			{
				stackPanel.Children.Add(GroupRow(group));
			}
			stackPanel.Opacity = ((section.Groups.Count != 0) ? 1 : 0);
			return stackPanel;
		}

		private FrameworkElement GroupRow(ClickyInkGroup group)
		{
			FrameworkElement frameworkElement = ClickyInkGroupView.Make(group, clock, Style, paper, actions);
			frameworkElement.Margin = new Thickness(frameworkElement.Margin.Left, frameworkElement.Margin.Top, frameworkElement.Margin.Right, frameworkElement.Margin.Bottom + 12.0);
			return ClickyInkGrow.Around(frameworkElement, clock, group.LandsAt, group.VanishesAt);
		}

		private FrameworkElement WideSection(ClickyInkDashboardBoard.Section section)
		{
			List<ClickyInkGroup> list = SpreadingLongList(section.Groups.Where((ClickyInkGroup g) => g.Kind != ClickyInkDocument.Kind.Table).ToList());
			StackPanel stackPanel = new StackPanel
			{
				Width = InnerWidth,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			stackPanel.Children.Add(Header(section, isMove: false));
			foreach (ClickyInkGroup item in section.Groups.Where((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Table))
			{
				FrameworkElement frameworkElement = ClickyInkTable.Create(item.Blocks[0], clock, Style, InnerWidth);
				frameworkElement.Margin = new Thickness(frameworkElement.Margin.Left, frameworkElement.Margin.Top, frameworkElement.Margin.Right, frameworkElement.Margin.Bottom + 12.0);
				stackPanel.Children.Add(ClickyInkGrow.Around(frameworkElement, clock, item.LandsAt, item.VanishesAt));
			}
			if (list.Count > 0)
			{
				stackPanel.Children.Add(new ClickyInkHStack(36.0, InkVerticalAlignment.Top, Groups(list.Where((ClickyInkGroup _, int i) => i % 2 == 0)), Groups(list.Where((ClickyInkGroup _, int i) => i % 2 == 1)))
				{
					HorizontalAlignment = HorizontalAlignment.Left
				});
			}
			return stackPanel;
		}

		private FrameworkElement Groups(IEnumerable<ClickyInkGroup> groups)
		{
			StackPanel stackPanel = new StackPanel
			{
				Width = 378.0,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			foreach (ClickyInkGroup group in groups)
			{
				stackPanel.Children.Add(GroupRow(group));
			}
			return stackPanel;
		}

		private FrameworkElement Footer(ClickyInkDashboardBoard board)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 6.0
			};
			stackPanel.Children.Add(new Border
			{
				Height = 1.0,
				Background = Ui.Brush(Style.Ink.Opacity(0.12))
			});
			ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
			{
				Spacing = 14.0,
				LineSpacing = 4.0
			};
			clickyNotebookFlow.Children.Add(ClickyInkFormKit.Text("sources", ClickyPaperFonts.Hand(15.0), Style.Quiet, 0.0, 1.0, 1));
			foreach (ClickyInkLiveNote.Block source in board.Sources)
			{
				if (clock >= source.LandsAt)
				{
					clickyNotebookFlow.Children.Add(ClickyInkFormKit.SourceLink(source, clock, Style, actions, 10.5, 11.5));
				}
			}
			stackPanel.Children.Add(clickyNotebookFlow);
			return stackPanel;
		}

		private FrameworkElement Sheet()
		{
			PaperSurface paperSurface = new PaperSurface(ClickyShape.RoundedRectangle(6.0), paper.Fill, 0.45).WithShadow(PaperColor.Black.Opacity(0.28), 12.0, 0.0, 5.0);
			paperSurface.Washes.Add(ClickyInkFormKit.Dots(18.0, 0.75));
			return paperSurface;
		}

		private static FrameworkElement Tape(double angle, double x, HorizontalAlignment alignment)
		{
			PaperSurface paperSurface = new PaperSurface(ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#F4EBD2").Opacity(0.78)), null).WithOutline(new PaperStroke(PaperColor.Black.Opacity(0.04), 0.5)).WithShadow(PaperColor.Black.Opacity(0.08), 1.0, 0.0, 1.0);
			paperSurface.Width = 70.0;
			paperSurface.Height = 20.0;
			paperSurface.HorizontalAlignment = alignment;
			paperSurface.VerticalAlignment = VerticalAlignment.Top;
			paperSurface.Transformed(x, 2.0, angle, 1.0, null);
			return paperSurface;
		}
	}

	public const double Margin = 36.0;

	public const double StruckWordsFoldAfter = 2.5;

	public static double CardWidth => 864.0;

	public static FrameworkElement Make(ClickyInkLiveNote note, double clock, ClickyHomeSpreadPaper? paper = null, ClickyInkActions? actions = null, double? width = null, int? runNumber = null, string? dateLine = null, Job? job = null, ClickyInkTiles.Look tilesLook = ClickyInkTiles.Look.Ruled)
	{
		FrameworkElement frameworkElement = new Builder(note, clock, paper ?? ClickyHomeSpreadPaper.Note, actions ?? new ClickyInkActions(), width ?? CardWidth, runNumber, dateLine, job, tilesLook).Body();
		ClickyInkFormKit.WithoutPenRenderer(frameworkElement);
		return frameworkElement;
	}

	public static List<ClickyInkGroup> SpreadingLongList(List<ClickyInkGroup> groups)
	{
		return ClickyInkDashboardSplit.SpreadingLongList(groups);
	}
}
