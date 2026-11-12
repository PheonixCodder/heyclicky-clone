using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkFrontPageView
{
	private sealed class Builder(ClickyInkLiveNote rawNote, double clock, ClickyInkFrontPageVariant variant, ClickyHomeSpreadPaper paper, ClickyInkActions actions, Size size, int? runNumber, string? dateLine)
	{
		[CompilerGenerated]
		private sealed class _003CTags_003Ed__31 : IEnumerable<FrameworkElement>, IEnumerable, IEnumerator<FrameworkElement>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private FrameworkElement _003C_003E2__current;

			private int _003C_003El__initialThreadId;

			private (int New, int Needs) counts;

			public (int New, int Needs) _003C_003E3__counts;

			FrameworkElement IEnumerator<FrameworkElement>.Current
			{
				[DebuggerHidden]
				get
				{
					return _003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return _003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CTags_003Ed__31(int _003C_003E1__state)
			{
				this._003C_003E1__state = _003C_003E1__state;
				_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				_003C_003E1__state = -2;
			}

			private bool MoveNext()
			{
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					if (counts.Needs > 0)
					{
						_003C_003E2__current = ClickyInkTag.Make("needs you", PaperColor.Hex("#B86A00"));
						_003C_003E1__state = 1;
						return true;
					}
					goto IL_005a;
				case 1:
					_003C_003E1__state = -1;
					goto IL_005a;
				case 2:
					{
						_003C_003E1__state = -1;
						break;
					}
					IL_005a:
					if (counts.New > 0)
					{
						_003C_003E2__current = ClickyInkTag.Make($"{counts.New} new", ClickyPaperInk.DoneGreen);
						_003C_003E1__state = 2;
						return true;
					}
					break;
				}
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<FrameworkElement> IEnumerable<FrameworkElement>.GetEnumerator()
			{
				_003CTags_003Ed__31 _003CTags_003Ed__;
				if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
				{
					_003C_003E1__state = 0;
					_003CTags_003Ed__ = this;
				}
				else
				{
					_003CTags_003Ed__ = new _003CTags_003Ed__31(0);
				}
				_003CTags_003Ed__.counts = _003C_003E3__counts;
				return _003CTags_003Ed__;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<FrameworkElement>)this).GetEnumerator();
			}
		}

		private readonly ClickyInkLiveNote note = rawNote.FoldingStruckWords(2.5);

		private ClickyInkStyle? style;

		private ClickyInkStyle Style => style ?? (style = new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock)));

		private bool IsMission => variant == ClickyInkFrontPageVariant.MissionControl;

		private bool IsPenWriting => ClickyInkPenSchedule.Of(note).IsWriting(clock);

		public FrameworkElement Body()
		{
			ClickyInkFrontPagePlanner.Plan plan = ClickyInkFrontPagePlans.For(note, variant, paper);
			ClickyInkDashboardBoard board = Board(note, clock);
			ClickyInkFrontPageContent clickyInkFrontPageContent = new ClickyInkFrontPageContent(note, board, plan, clock);
			double num = Math.Min(size.Width / plan.Canvas.Width, size.Height / plan.Canvas.Height);
			Canvas canvas = new Canvas
			{
				Width = plan.Canvas.Width,
				Height = plan.Canvas.Height
			};
			Place(Sheet(plan), 0.0, 0.0, plan.Canvas.Width, plan.Canvas.Height);
			Place(Masthead(board, plan), plan.Masthead.X, plan.Masthead.Y, plan.InnerWidth, plan.Masthead.Height);
			Place(Headline(board, plan), plan.Headline.X, plan.Headline.Y, plan.Headline.Width, plan.Headline.Height);
			Rect? stats = plan.Stats;
			if (stats.HasValue)
			{
				Rect valueOrDefault = stats.GetValueOrDefault();
				Place(Stats(clickyInkFrontPageContent.StatBlocks, valueOrDefault), valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Width, valueOrDefault.Height);
			}
			foreach (ClickyInkFrontPagePlanner.Band band in plan.Bands)
			{
				FrameworkElement frameworkElement = BandView(band, clickyInkFrontPageContent);
				if ((object)frameworkElement != null)
				{
					Place(frameworkElement, band.Frame.X, band.Frame.Y, band.Frame.Width, band.Frame.Height);
				}
			}
			stats = plan.Move;
			if (stats.HasValue)
			{
				Rect valueOrDefault2 = stats.GetValueOrDefault();
				Place(Move(clickyInkFrontPageContent.MoveGroups, valueOrDefault2), valueOrDefault2.X, valueOrDefault2.Y, valueOrDefault2.Width, null);
			}
			if (!IsMission)
			{
				for (int i = 0; i < plan.Rules.Count; i++)
				{
					Rect rect = plan.Rules[i];
					Place(ClickyShapeView.Stroke(new ClickyInkRuledLine(new Point(0.5, 0.0), new Point(0.5, 1.0), 20 + i), PaperStroke.Round(paper.Ink.Opacity(0.22), 1.0)), rect.X, rect.Y, 1.0, Math.Max(0.0, rect.Height));
				}
			}
			foreach (ClickyInkFrontPagePlanner.Column item in plan.Flow)
			{
				StackPanel stackPanel = new StackPanel
				{
					Width = plan.ColumnWidth
				};
				foreach (ClickyInkFrontPagePlanner.Chunk chunk in item.Chunks)
				{
					stackPanel.Children.Add(ChunkView(chunk, clickyInkFrontPageContent, plan, item.ExtraGap));
				}
				Place(stackPanel, item.X, item.Top, null, null);
			}
			Place(Footer(board, plan), plan.Footer.X, plan.Footer.Y, plan.InnerWidth, plan.Footer.Height);
			canvas.RenderTransform = new ScaleTransform
			{
				ScaleX = num,
				ScaleY = num
			};
			return new Grid
			{
				Width = size.Width,
				Height = size.Height,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Clip = new RectangleGeometry
				{
					Rect = new Rect(0.0, 0.0, size.Width, size.Height)
				},
				Children = { (UIElement)canvas }
			};
			void Place(FrameworkElement e, double x, double y, double? width = null, double? height = null)
			{
				if (width.HasValue)
				{
					double valueOrDefault3 = width.GetValueOrDefault();
					e.Width = Math.Max(0.0, valueOrDefault3);
				}
				if (height.HasValue)
				{
					double valueOrDefault4 = height.GetValueOrDefault();
					e.Height = Math.Max(0.0, valueOrDefault4);
				}
				Canvas.SetLeft(e, x);
				Canvas.SetTop(e, y);
				canvas.Children.Add(e);
			}
		}

		private FrameworkElement Sheet(ClickyInkFrontPagePlanner.Plan plan)
		{
			if (IsMission)
			{
				return new PaperSurface(ClickyShape.RoundedRectangle(8.0), paper.Fill, 0.45)
				{
					Washes = { ClickyInkFormKit.Dots(22.0, 1.0) }
				};
			}
			double middle = plan.Canvas.Width / 2.0;
			return new PaperSurface(ClickyShape.RoundedRectangle(8.0), PaperFill.Of(PaperColor.Hex("#F4EEE0")), 0.6).WithWash(PaperFill.Horizontal((0.0, PaperColor.Clear), (1.0, PaperColor.Black.Opacity(0.05))), (Size s) => new Rect(middle - 35.0 - 5.0, 0.0, 40.0, s.Height)).WithWash(PaperFill.Horizontal((0.0, PaperColor.Black.Opacity(0.07)), (1.0, PaperColor.Clear)), (Size s) => new Rect(middle + 5.0 - 5.0, 0.0, 30.0, s.Height));
		}

		private FrameworkElement Masthead(ClickyInkDashboardBoard board, ClickyInkFrontPagePlanner.Plan plan)
		{
			ClickyInkLiveNote.Block run = board.Run;
			ClickyInkRunLine clickyInkRunLine = ((run != null) ? new ClickyInkRunLine(run.Source) : null);
			string number = clickyInkRunLine?.Number ?? runNumber?.ToString(CultureInfo.InvariantCulture);
			int num2;
			if (board.Run == null)
			{
				int? num = runNumber;
				num2 = (num.HasValue ? 1 : 0);
			}
			else
			{
				num2 = 1;
			}
			bool flag = (byte)num2 != 0;
			string text = clickyInkRunLine?.RanAt ?? dateLine;
			if (IsMission)
			{
				ClickyInkHStack clickyInkHStack = new ClickyInkHStack(16.0, InkVerticalAlignment.Center)
				{
					Margin = new Thickness(18.0, 0.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Center
				};
				clickyInkHStack.Children.Add(LiveLight(board));
				ClickyInkLiveNote.Words title = note.Title;
				if ((object)title != null)
				{
					clickyInkHStack.Children.Add(ClickyInkWordsView.Make(title, clock, Style, ClickyPaperFonts.Hand(40.0), pencil: true, null, null).With(delegate(InkTextView t)
					{
						t.LineLimit = 1;
						t.MinimumScale = 0.6;
					}));
				}
				clickyInkHStack.Children.Add(ClickyInkFormKit.Text(flag ? "KEPT BY CLICKY" : "MADE BY CLICKY", ClickyPaperFonts.Typed(11.0, FontWeights.Bold), Style.Quiet, 1.6, 1.0, 1));
				clickyInkHStack.Children.Add(new ClickyInkSpacer());
				FrameworkElement frameworkElement = ClickyInkFormKit.Changes(board.Changes, Style, 16.0, 6.0);
				if ((object)frameworkElement != null)
				{
					clickyInkHStack.Children.Add(frameworkElement);
				}
				string text2 = clickyInkRunLine?.Next;
				if (text2 != null)
				{
					clickyInkHStack.Children.Add(ClickyInkFormKit.Text("next run " + text2, ClickyPaperFonts.Hand(17.0), Style.Quiet, 0.0, 1.0, 1));
				}
				if (text != null)
				{
					clickyInkHStack.Children.Add(ClickyInkFormKit.Text(text, ClickyPaperFonts.Typed(13.0, FontWeights.SemiBold), Style.Ink.Opacity(0.7), 0.0, 1.0, 1));
				}
				FrameworkElement frameworkElement2 = Stamp(board, number, 15.0);
				if ((object)frameworkElement2 != null)
				{
					clickyInkHStack.Children.Add(frameworkElement2);
				}
				return new Grid
				{
					Height = plan.Masthead.Height,
					Children = 
					{
						(UIElement)PanelCard(PaperColor.Hex("#FFFDF6"), ClickyInkDashboardFormNeeds.Lamp(board.Overall), 1),
						(UIElement)clickyInkHStack
					}
				};
			}
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 5.0
			};
			Grid grid = new Grid
			{
				Height = 64.0
			};
			ClickyInkLiveNote.Words title2 = note.Title;
			if ((object)title2 != null)
			{
				InkTextView inkTextView = ClickyInkWordsView.Make(title2, clock, Style, ClickyPaperFonts.Hand(58.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.5;
				});
				inkTextView.MaxWidth = plan.InnerWidth * 0.62;
				inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
				inkTextView.VerticalAlignment = VerticalAlignment.Center;
				grid.Children.Add(inkTextView);
			}
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 3.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel2.Children.Add(ClickyInkFormKit.Text(flag ? "KEPT BY CLICKY" : "MADE BY CLICKY", ClickyPaperFonts.Typed(11.0, FontWeights.Bold), Style.Quiet, 1.6, 1.0, 1));
			string text3 = clickyInkRunLine?.Next;
			if (text3 != null)
			{
				stackPanel2.Children.Add(ClickyInkFormKit.Text(("NEXT RUN " + text3).ToUpperInvariant(), ClickyPaperFonts.Typed(10.0, FontWeights.Bold), Style.Quiet, 1.2, 1.0, 1));
			}
			ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, stackPanel2, new ClickyInkSpacer());
			FrameworkElement frameworkElement3 = Stamp(board, number, 14.0);
			if ((object)frameworkElement3 != null)
			{
				clickyInkHStack2.Children.Add(frameworkElement3);
			}
			clickyInkHStack2.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(clickyInkHStack2);
			stackPanel.Children.Add(grid);
			stackPanel.Children.Add(Ruled(doubled: true));
			List<string> values = (from p in note.Pages
				select ClickyInkParser.Emphasis.Strip(p.Name.Current) into n
				where n.Length > 0
				select n).ToList();
			string text4 = (flag ? string.Join("  ·  ", new string[2]
			{
				text,
				clickyInkRunLine?.Extra
			}.Where((string s) => s != null)) : ("inside: " + string.Join(" · ", values)));
			ClickyInkHStack clickyInkHStack3 = new ClickyInkHStack(14.0, InkVerticalAlignment.Center, ClickyInkFormKit.Text(text4.ToUpperInvariant(), ClickyPaperFonts.Typed(11.0, FontWeights.Bold), Style.Quiet, 1.3, 1.0, 1), new ClickyInkSpacer())
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			FrameworkElement frameworkElement4 = ClickyInkFormKit.Changes(board.Changes, Style, 16.0, 6.0);
			if ((object)frameworkElement4 != null)
			{
				clickyInkHStack3.Children.Add(frameworkElement4);
			}
			stackPanel.Children.Add(new Grid
			{
				Height = 22.0,
				Children = { (UIElement)clickyInkHStack3 }
			});
			stackPanel.Children.Add(Ruled(doubled: false));
			return stackPanel;
		}

		private FrameworkElement? Stamp(ClickyInkDashboardBoard board, string? number, double fontSize)
		{
			ClickyInkLiveNote.Block run = board.Run;
			if (run != null)
			{
				return ClickyInkDashboardFormNeeds.RunStampMark(number, run.Source, clock, run.LandsAt, fontSize);
			}
			if (number != null)
			{
				return new ClickyPaperStamp(ClickyThingStatus.Watching, "RUN " + number, fontSize, -5.0);
			}
			if (clock >= note.FinishedClock - 0.6 && !IsPenWriting)
			{
				return new ClickyPaperStamp(ClickyThingStatus.Ready, "DONE", fontSize, -5.0);
			}
			return null;
		}

		private FrameworkElement LiveLight(ClickyInkDashboardBoard board)
		{
			bool isPenWriting = IsPenWriting;
			Color c = (isPenWriting ? ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatusState.Bad) : ClickyInkDashboardFormNeeds.Lamp(board.Overall));
			Grid grid = new Grid
			{
				Width = 12.0,
				Height = 12.0
			};
			grid.Children.Add(new Ellipse
			{
				Fill = Ui.Brush(c)
			});
			if (isPenWriting)
			{
				Ellipse ellipse = new Ellipse
				{
					Stroke = Ui.Brush(c.Opacity(0.35)),
					StrokeThickness = 5.0,
					Margin = new Thickness(-2.5)
				};
				ellipse.Transformed(0.0, 0.0, 0.0, 1.0 + 0.25 * (Math.Sin(clock * 5.0) + 1.0), null);
				grid.Children.Add(ellipse);
			}
			InkTextView inkTextView = ClickyInkFormKit.Text(isPenWriting ? "LIVE" : ((board.Overall == ClickyInkStatusState.Ok) ? "ALL GOOD" : ((board.Overall == ClickyInkStatusState.Bad) ? "ALERT" : "NEEDS YOU")), ClickyPaperFonts.Typed(12.0, FontWeights.ExtraBold), isPenWriting ? PaperColor.Hex("#C2302B") : ClickyInkDashboardFormNeeds.Word(board.Overall), 1.4, 1.0, 1);
			return new ClickyInkOverlay(new ClickyInkHStack(7.0, InkVerticalAlignment.Center, grid, inkTextView)
			{
				Margin = new Thickness(10.0, 5.0, 10.0, 5.0)
			}, ClickyShapeView.Stroke(ClickyShape.Capsule, c.Opacity(0.5), 1.2))
			{
				VerticalAlignment = VerticalAlignment.Center
			};
		}

		private FrameworkElement Ruled(bool doubled)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 2.5
			};
			stackPanel.Children.Add(new Border
			{
				Height = (doubled ? 1.6 : 0.8),
				Background = Ui.Brush(Style.Ink.Opacity(0.8))
			});
			if (doubled)
			{
				stackPanel.Children.Add(new Border
				{
					Height = 0.8,
					Background = Ui.Brush(Style.Ink.Opacity(0.8))
				});
			}
			return stackPanel;
		}

		private FrameworkElement Headline(ClickyInkDashboardBoard board, ClickyInkFrontPagePlanner.Plan plan)
		{
			double width = plan.Headline.Width - (double)(IsMission ? 36 : 0);
			FrameworkElement frameworkElement = null;
			ClickyInkGroup quiet = board.Quiet;
			if ((object)quiet != null)
			{
				frameworkElement = ClickyInkGrow.Around(QuietLine(quiet.Blocks[0], board.Answer?.Blocks.FirstOrDefault(), width), clock, quiet.LandsAt, quiet.VanishesAt);
			}
			else
			{
				ClickyInkGroup answer = board.Answer;
				if ((object)answer != null)
				{
					frameworkElement = ClickyInkGrow.Around(AnswerLine(answer.Blocks[0], board, width), clock, answer.LandsAt, answer.VanishesAt);
				}
				else
				{
					string leadFallbackTitle = plan.LeadFallbackTitle;
					if (leadFallbackTitle != null)
					{
						frameworkElement = ClickyInkFormKit.Text(leadFallbackTitle, ClickyPaperFonts.Hand(30.0), Style.Quiet, 0.0, 1.0, 1);
					}
				}
			}
			Grid grid = new Grid();
			if (IsMission)
			{
				FrameworkElement frameworkElement2 = PanelCard(PaperColor.Hex("#FFFDF6"), ClickyInkDashboardFormNeeds.Lamp(board.Overall), 2);
				frameworkElement2.Opacity = (((object)board.Answer != null || (object)board.Quiet != null) ? 1 : 0);
				grid.Children.Add(frameworkElement2);
			}
			if ((object)frameworkElement != null)
			{
				frameworkElement.Margin = new Thickness(IsMission ? 18 : 0);
				frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
				frameworkElement.VerticalAlignment = (IsMission ? VerticalAlignment.Center : VerticalAlignment.Top);
				grid.Children.Add(frameworkElement);
			}
			return grid;
		}

		private FrameworkElement QuietLine(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Block? lastNews, double width)
		{
			ClickyInkQuietReading clickyInkQuietReading = new ClickyInkQuietReading(block.Source);
			double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.05, block.LandsAt + 0.35);
			Grid grid = new Grid
			{
				Children = { (UIElement)new ClickyPaperStamp(ClickyThingStatus.Ready, "ALL QUIET", 22.0) }
			};
			grid.Transformed(0.0, 0.0, 0.0, 1.0 + 0.35 * (1.0 - num), null);
			grid.Opacity = ((num > 0.0) ? (0.3 + 0.7 * num) : 0.0);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 4.0
			};
			stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, Style, ClickyPaperFonts.Hand(IsMission ? 34 : 42), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
			string checkedLine = clickyInkQuietReading.CheckedLine;
			if (checkedLine != null)
			{
				stackPanel.Children.Add(ClickyInkFormKit.Text(checkedLine, ClickyPaperFonts.Typed(14.0, FontWeights.Medium), Style.Quiet, 0.0, 1.0, 1));
			}
			if (lastNews != null)
			{
				ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkFormKit.Pill("last news", ClickyPaperFonts.Hand(17.0), Style.Quiet, Style.Quiet.Opacity(0.5), 5.0), ClickyInkWordsView.Make(lastNews.Words, clock, Style, ClickyPaperFonts.Hand(21.0), pencil: false, Style.Ink.Opacity(0.75), null).With(delegate(InkTextView t)
				{
					t.LineLimit = 2;
				}));
				clickyInkHStack.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
				stackPanel.Children.Add(clickyInkHStack);
			}
			return new ClickyInkHStack(22.0, InkVerticalAlignment.Center, grid, ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer())
			{
				Width = width
			};
		}

		private FrameworkElement AnswerLine(ClickyInkLiveNote.Block block, ClickyInkDashboardBoard board, double width)
		{
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
			int num4 = block.Words.Current.CharacterCount();
			double size = ((!IsMission) ? ((num4 > 80) ? 36 : ((num4 > 50) ? 42 : 48)) : ((num4 > 70) ? 30 : 36));
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(22.0, InkVerticalAlignment.Center)
			{
				Width = width
			};
			if (IsMission)
			{
				clickyInkHStack.Children.Add(ClickyInkDashboardFormNeeds.LampView(board.Overall, 28.0, 4, ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.5)));
			}
			bool flag = block.IsStruck(clock);
			InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, Style, ClickyPaperFonts.Hand(size), pencil: true, flag ? new Color?(Style.Quiet) : ((Color?)null), null);
			if (flag)
			{
				inkTextView.Strikethrough = Style.Fresh;
			}
			inkTextView.LineSpacing = -2.0;
			clickyInkHStack.Children.Add(inkTextView);
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			string text = block.Source["big"];
			if (text != null && text.Length > 0)
			{
				FrameworkElement frameworkElement = ClickyInkDashboardFormNeeds.TickingNumber(block.Source, "big", clock, Style, (text.CharacterCount() > 6) ? 46 : 60, Math.Max(0.0, num3 - (num ?? num3)), text, ClickyInkChange.Previous(block.Source, "big") ?? block.Source["_was"], showsWas: false);
				frameworkElement.Margin = new Thickness(12.0, 4.0, 12.0, 4.0);
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(text.CharacterCount()).Trim(0.0, ClickyInkClock.Progress(clock, num3, num3 + 0.5)), PaperStroke.Round(Style.Fresh.Opacity(0.8), 2.8));
				clickyShapeView.Margin = new Thickness(-5.0);
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 2.0
				};
				stackPanel.Children.Add(new ClickyInkOverlay(frameworkElement, clickyShapeView)
				{
					HorizontalAlignment = HorizontalAlignment.Center
				});
				string text2 = block.Source["note"];
				if (text2 != null && text2.Length > 0)
				{
					InkTextView inkTextView2 = ClickyInkFormKit.Text((num.HasValue && clock < num3) ? (ClickyInkChange.Previous(block.Source, "note") ?? text2) : text2, ClickyPaperFonts.Hand(19.0), Style.Fresh, 0.0, 1.0, 2).With(delegate(InkTextView t)
					{
						t.TextAlignment = TextAlignment.Center;
					});
					inkTextView2.MaxWidth = 220.0;
					inkTextView2.HorizontalAlignment = HorizontalAlignment.Center;
					inkTextView2.Opacity = ((clock >= num3) ? 1 : 0);
					stackPanel.Children.Add(inkTextView2);
				}
				clickyInkHStack.Children.Add(stackPanel);
			}
			return clickyInkHStack;
		}

		private FrameworkElement Stats(List<ClickyInkLiveNote.Block> blocks, Rect frame)
		{
			if (IsMission)
			{
				int num = ClickyInkFrontPagePlanner.MissionTilesPerRow(blocks.Count, frame.Width);
				double width = (frame.Width - (double)((num - 1) * 14)) / (double)num;
				double height = ClickyInkFrontPagePlans.MissionTileHeight(blocks.Select((ClickyInkLiveNote.Block b) => b.Source).ToList());
				StackPanel stackPanel = new StackPanel
				{
					Spacing = 14.0
				};
				for (int i = 0; i * num < blocks.Count; i++)
				{
					StackPanel stackPanel2 = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 14.0
					};
					List<ClickyInkLiveNote.Block> list = blocks.Skip(i * num).Take(num).ToList();
					for (int j = 0; j < list.Count; j++)
					{
						ClickyInkLiveNote.Block block = list[j];
						double num2 = ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.5);
						FrameworkElement frameworkElement = ClickyInkDashboardFormNeeds.Tile(block, clock, Style, i * num + j);
						frameworkElement.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
						frameworkElement.VerticalAlignment = VerticalAlignment.Top;
						frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
						Grid grid = new Grid
						{
							Width = width,
							Height = height
						};
						grid.Children.Add(PanelCard(PaperColor.Hex("#FFFDF6"), TileAccent(block), j + i * 5));
						grid.Children.Add(frameworkElement);
						grid.Opacity = ((clock >= block.LandsAt) ? Math.Min(1.0, num2 * 2.0) : 0.0);
						stackPanel2.Children.Add(grid);
					}
					stackPanel.Children.Add(stackPanel2);
				}
				return stackPanel;
			}
			StackPanel obj = new StackPanel
			{
				Spacing = 4.0
			};
			InkTextView inkTextView = ClickyInkFormKit.Text("BY THE NUMBERS", ClickyPaperFonts.Typed(10.5, FontWeights.ExtraBold), Style.Quiet, 1.8, 1.0, 1);
			inkTextView.Opacity = ((blocks.Count != 0) ? 1 : 0);
			obj.Children.Add(inkTextView);
			obj.Children.Add(ClickyInkDashboardFormNeeds.Tiles(blocks, clock, Style, frame.Width));
			return obj;
		}

		private Color TileAccent(ClickyInkLiveNote.Block block)
		{
			ClickyInkDocument.Block source = block.Source;
			if (source.Kind == ClickyInkDocument.Kind.Status)
			{
				return ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatus.State(source["state"] ?? source["status"]));
			}
			string text = source["delta"];
			if (text != null && (text.StartsWith('-') || text.StartsWith('−')))
			{
				return ClickyInkDashboardFormNeeds.Down;
			}
			string text2 = source["delta"];
			if (text2 != null && text2.StartsWith('+'))
			{
				return ClickyInkDashboardFormNeeds.Up;
			}
			return Style.Fresh;
		}

		private FrameworkElement Move(List<ClickyInkGroup> groups, Rect frame)
		{
			double num = ((groups.Count == 0) ? double.PositiveInfinity : groups.Min((ClickyInkGroup g) => g.LandsAt));
			StackPanel stackPanel = new StackPanel();
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
			if (IsMission)
			{
				clickyInkHStack.Children.Add(ClickyInkDashboardFormNeeds.LampView(ClickyInkStatusState.Watch, 16.0, 7).BaselineFromBottom(3.0));
			}
			clickyInkHStack.Children.Add(ClickyInkFormKit.Text("your move", ClickyPaperFonts.Hand(IsMission ? 23 : 26), PaperColor.Hex("#B05F00"), 0.0, 1.0, 1));
			clickyInkHStack.Children.Add(ClickyInkFormKit.Squiggle(5, PaperColor.Hex("#B05F00").Opacity(0.3)));
			clickyInkHStack.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
			stackPanel.Children.Add(clickyInkHStack);
			foreach (ClickyInkGroup group in groups)
			{
				stackPanel.Children.Add(PieceView(group, 0.0));
			}
			stackPanel.Margin = new Thickness(IsMission ? 16.0 : 0.0, 16.0, IsMission ? 16.0 : 0.0, 4.0);
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel)
			{
				Width = frame.Width
			};
			if (IsMission)
			{
				clickyInkOverlay.Behind(PanelCard(PaperColor.Hex("#FFF6DA"), ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatusState.Watch), 9));
			}
			else
			{
				Grid grid = new Grid
				{
					Margin = new Thickness(-12.0, 0.0, -12.0, 0.0)
				};
				grid.Children.Add(new Border
				{
					Background = Ui.Brush(PaperColor.Hex("#FFF3D1").Opacity(0.55))
				});
				grid.Children.Add(ClickyShapeView.Stroke(new ClickyInkRuledBox(6).Trim(0.0, ClickyInkClock.Progress(clock, num, num + 0.6)), PaperStroke.Round(PaperColor.Hex("#B05F00").Opacity(0.6), 1.4)));
				clickyInkOverlay.Behind(grid);
			}
			clickyInkOverlay.Opacity = ((clock >= num) ? 1 : 0);
			return clickyInkOverlay;
		}

		private FrameworkElement PieceView(ClickyInkGroup group, double extraGap)
		{
			FrameworkElement item = ClickyInkGroupView.Make(group, clock, Style, paper, actions);
			return ClickyInkGrow.Around(new Grid
			{
				Width = 378.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children = { (UIElement)item },
				Margin = new Thickness(0.0, 0.0, 0.0, 12.0 + extraGap)
			}, clock, group.LandsAt, group.VanishesAt);
		}

		private FrameworkElement ChunkView(ClickyInkFrontPagePlanner.Chunk chunk, ClickyInkFrontPageContent content, ClickyInkFrontPagePlanner.Plan plan, double extraGap)
		{
			List<ClickyInkGroup> list = content.Groups(chunk);
			double num = ((list.Count == 0) ? double.PositiveInfinity : list.Min((ClickyInkGroup g) => g.LandsAt));
			ClickyInkLiveNote.Words words = content.StoryName(chunk.StoryID);
			Color color = Accent(content, chunk.StoryID);
			StackPanel stackPanel = new StackPanel();
			if (IsMission)
			{
				ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
				clickyInkHStack.Children.Add(new Ellipse
				{
					Width = 9.0,
					Height = 9.0,
					Fill = Ui.Brush(color)
				});
				if ((object)words != null)
				{
					clickyInkHStack.Children.Add(ClickyInkWordsView.Make(words, clock, Style, ClickyPaperFonts.Hand(21.0), pencil: true, null, null).With(delegate(InkTextView t)
					{
						t.LineLimit = 1;
					}));
				}
				if (chunk.IsContinuation)
				{
					clickyInkHStack.Children.Add(ClickyInkFormKit.Text("cont.", ClickyPaperFonts.Hand(15.0), Style.Quiet, 0.0, 1.0, 1));
				}
				clickyInkHStack.Children.Add(new ClickyInkSpacer());
				foreach (FrameworkElement item in Tags(content.Counts(chunk.StoryID, clock)))
				{
					clickyInkHStack.Children.Add(item);
				}
				clickyInkHStack.VerticalAlignment = VerticalAlignment.Center;
				stackPanel.Children.Add(new Grid
				{
					Height = 30.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
					Children = { (UIElement)clickyInkHStack }
				});
			}
			else if (chunk.IsContinuation)
			{
				InkTextView inkTextView = ClickyInkFormKit.Text((words?.Current.ToLowerInvariant() ?? "continued") + ", continued", ClickyPaperFonts.Hand(16.0), Style.Quiet, 0.0, 1.0, 1);
				inkTextView.VerticalAlignment = VerticalAlignment.Top;
				stackPanel.Children.Add(new Grid
				{
					Height = 24.0,
					Children = { (UIElement)inkTextView }
				});
			}
			else if (!chunk.IsLead && (object)words != null)
			{
				StackPanel stackPanel2 = new StackPanel
				{
					Spacing = 4.0,
					VerticalAlignment = VerticalAlignment.Top
				};
				stackPanel2.Children.Add(new Border
				{
					Height = 1.2,
					Background = Ui.Brush(paper.Ink.Opacity(0.55))
				});
				ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkWordsView.Make(words, clock, Style, ClickyPaperFonts.Hand(25.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
				foreach (FrameworkElement item2 in Tags(content.Counts(chunk.StoryID, clock)))
				{
					clickyInkHStack2.Children.Add(item2);
				}
				clickyInkHStack2.Children.Add(new ClickyInkSpacer());
				stackPanel2.Children.Add(clickyInkHStack2);
				stackPanel.Children.Add(new Grid
				{
					Height = 44.0,
					Children = { (UIElement)stackPanel2 }
				});
			}
			foreach (ClickyInkGroup item3 in list)
			{
				stackPanel.Children.Add(PieceView(item3, extraGap));
			}
			if (IsMission)
			{
				stackPanel.Children.Add(new Grid
				{
					Height = 4.0
				});
			}
			stackPanel.Margin = new Thickness(IsMission ? 16.0 : 0.0, IsMission ? 8 : 0, IsMission ? 16.0 : 0.0, 0.0);
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel)
			{
				Width = plan.ColumnWidth
			};
			if (IsMission)
			{
				clickyInkOverlay.Behind(PanelCard(PaperColor.Hex("#FFFDF6"), color, chunk.StoryID.CharacterCount() + (chunk.IsContinuation ? 3 : 0)));
			}
			clickyInkOverlay.Opacity = ((clock >= num) ? 1 : 0);
			FrameworkElement frameworkElement = ClickyInkGrow.Around(clickyInkOverlay, clock, num, null);
			frameworkElement.Margin = new Thickness(0.0, 0.0, 0.0, plan.StoryGap + extraGap);
			return frameworkElement;
		}

		private FrameworkElement? BandView(ClickyInkFrontPagePlanner.Band band, ClickyInkFrontPageContent content)
		{
			ClickyInkGroup clickyInkGroup = content.Group(band.GroupID);
			if ((object)clickyInkGroup == null)
			{
				return null;
			}
			ClickyInkLiveNote.Words words = content.StoryName(band.StoryID);
			StackPanel stackPanel = new StackPanel();
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline);
			if (IsMission)
			{
				clickyInkHStack.Children.Add(new Ellipse
				{
					Width = 9.0,
					Height = 9.0,
					Fill = Ui.Brush(Accent(content, band.StoryID))
				});
			}
			if ((object)words != null)
			{
				clickyInkHStack.Children.Add(ClickyInkWordsView.Make(words, clock, Style, ClickyPaperFonts.Hand(IsMission ? 21 : 25), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
			foreach (FrameworkElement item in Tags(content.Counts(band.StoryID, clock)))
			{
				clickyInkHStack.Children.Add(item);
			}
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			clickyInkHStack.VerticalAlignment = VerticalAlignment.Bottom;
			Grid grid = new Grid
			{
				Height = 36.0,
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
				Children = { (UIElement)clickyInkHStack }
			};
			if (!IsMission)
			{
				grid.Children.Add(new Border
				{
					Height = 1.2,
					VerticalAlignment = VerticalAlignment.Top,
					Background = Ui.Brush(paper.Ink.Opacity(0.55))
				});
			}
			stackPanel.Children.Add(grid);
			FrameworkElement view = ClickyInkTable.Create(clickyInkGroup.Blocks[0], clock, Style, band.Frame.Width - (IsMission ? 32.0 : 0.0));
			stackPanel.Children.Add(ClickyInkGrow.Around(view, clock, clickyInkGroup.LandsAt, clickyInkGroup.VanishesAt));
			stackPanel.Margin = new Thickness(IsMission ? 16.0 : 0.0, IsMission ? 4 : 0, IsMission ? 16.0 : 0.0, 0.0);
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel)
			{
				VerticalAlignment = VerticalAlignment.Top
			};
			if (IsMission)
			{
				clickyInkOverlay.Behind(PanelCard(PaperColor.Hex("#FFFDF6"), Accent(content, band.StoryID), band.GroupID.CharacterCount()));
			}
			clickyInkOverlay.Opacity = ((clock >= clickyInkGroup.LandsAt) ? 1 : 0);
			return clickyInkOverlay;
		}

		[IteratorStateMachine(typeof(_003CTags_003Ed__31))]
		private IEnumerable<FrameworkElement> Tags((int New, int Needs) counts)
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CTags_003Ed__31(-2)
			{
				_003C_003E3__counts = counts
			};
		}

		private Color Accent(ClickyInkFrontPageContent content, string storyID)
		{
			(int, int) tuple = content.Counts(storyID, clock);
			if (tuple.Item2 > 0)
			{
				return ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatusState.Watch);
			}
			if (tuple.Item1 > 0)
			{
				return ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatusState.Ok);
			}
			if (content.HasBadStatus(storyID))
			{
				return ClickyInkDashboardFormNeeds.Lamp(ClickyInkStatusState.Bad);
			}
			return Style.Fresh.Opacity(0.75);
		}

		private FrameworkElement Footer(ClickyInkDashboardBoard board, ClickyInkFrontPagePlanner.Plan plan)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 5.0
			};
			if (!IsMission)
			{
				stackPanel.Children.Add(new Border
				{
					Height = 0.8,
					Background = Ui.Brush(Style.Ink.Opacity(0.5))
				});
			}
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(14.0, InkVerticalAlignment.FirstBaseline);
			clickyInkHStack.Children.Add(ClickyInkFormKit.Text(IsMission ? "FEED" : "SOURCES", ClickyPaperFonts.Typed(10.5, FontWeights.ExtraBold), Style.Quiet, 1.6, 1.0, 1));
			foreach (ClickyInkLiveNote.Block item in board.Sources.Take(6))
			{
				if (clock >= item.LandsAt)
				{
					clickyInkHStack.Children.Add(ClickyInkFormKit.SourceLink(item, clock, Style, actions, 11.0, 12.5));
				}
			}
			if (plan.OverflowNames.Count > 0)
			{
				clickyInkHStack.Children.Add(ClickyInkFormKit.Text("also inside: " + string.Join(" · ", plan.OverflowNames), ClickyPaperFonts.Hand(16.0), Style.Quiet, 0.0, 1.0, 1));
			}
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			stackPanel.Children.Add(clickyInkHStack);
			stackPanel.Margin = new Thickness(IsMission ? 14 : 0, IsMission ? 6 : 0, IsMission ? 14 : 0, IsMission ? 6 : 0);
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel)
			{
				VerticalAlignment = VerticalAlignment.Top
			};
			if (IsMission && board.Sources.Count > 0)
			{
				clickyInkOverlay.Behind(PanelCard(PaperColor.Hex("#FFFDF6"), Style.Quiet, 11));
			}
			clickyInkOverlay.Opacity = ((board.Sources.Count != 0 || plan.OverflowNames.Count != 0) ? 1 : 0);
			return clickyInkOverlay;
		}

		private static FrameworkElement PanelCard(Color tint, Color accent, int seed)
		{
			return new Grid
			{
				Children = 
				{
					(UIElement)new PaperSurface(ClickyShape.RoundedRectangle(7.0), PaperFill.Of(tint), 0.3).WithWash(PaperFill.Of(accent.Opacity(0.85)), (Size s) => new Rect(0.0, 0.0, s.Width, 4.0)).WithShadow(PaperColor.Black.Opacity(0.16), 4.0, 0.0, 2.0),
					(UIElement)ClickyShapeView.Stroke(new ClickyInkRuledBox(seed, 0.0), ClickyInkDashboardFormNeeds.Pencil.Opacity(0.18), 1.0)
				}
			};
		}
	}

	public static Size SpreadSize => ClickyInkFrontPagePlanner.SpreadSize;

	public static ClickyInkDashboardBoard Board(ClickyInkLiveNote note, double clock)
	{
		return ClickyInkDashboardBoard.ForFrontPage(note, clock);
	}

	public static FrameworkElement Make(ClickyInkLiveNote note, double clock, ClickyInkFrontPageVariant variant = ClickyInkFrontPageVariant.Broadsheet, ClickyHomeSpreadPaper? paper = null, ClickyInkActions? actions = null, Size? size = null, int? runNumber = null, string? dateLine = null)
	{
		return new Builder(note, clock, variant, paper ?? ClickyHomeSpreadPaper.Note, actions ?? new ClickyInkActions(), size ?? SpreadSize, runNumber, dateLine).Body();
	}
}
