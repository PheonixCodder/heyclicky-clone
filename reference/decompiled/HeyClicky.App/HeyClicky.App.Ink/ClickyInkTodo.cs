using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTodo
{
	private sealed class State : InkObjectState
	{
		public int? ShownDone;

		public double FromFraction;

		public double ChangedAt = -1000.0;

		public readonly Dictionary<string, bool> ShownTicks = new Dictionary<string, bool>();

		public readonly Dictionary<string, double> TickChangedAt = new Dictionary<string, double>();
	}

	private sealed class ProgressStrip : Grid
	{
		public ProgressStrip(double fraction)
		{
			Border item = new Border
			{
				CornerRadius = new CornerRadius(3.5),
				Background = Ui.Brush(Ink.Opacity(0.07))
			};
			Border done = new Border
			{
				CornerRadius = new CornerRadius(3.5),
				Background = Ui.Brush(PaperColor.Hex("#A6D85A").Opacity(0.55)),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			base.Children.Add(item);
			base.Children.Add(done);
			base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				done.Width = Math.Max(0.0, e.NewSize.Width * fraction);
			};
		}
	}

	private static readonly Color Ink = PaperColor.Hex("#2E2A1F");

	private static readonly Color RuleBlue = PaperColor.Hex("#6E9BD6").Opacity(0.25);

	private static readonly Color MarginRed = PaperColor.Hex("#E5484D").Opacity(0.28);

	private const double TickResponse = 0.3;

	private const double TickDamping = 0.7;

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "todo");
		return new InkObjectHost(state, () => Build(block, clock, style, actions, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, State state)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		int num = block.Items.Count((ClickyInkLiveNote.Item i) => i.Source.IsChecked == true);
		double num2 = ((block.Items.Count == 0) ? 0.0 : ((double)num / (double)block.Items.Count));
		int? shownDone = state.ShownDone;
		if (shownDone.HasValue)
		{
			int valueOrDefault = shownDone.GetValueOrDefault();
			if (valueOrDefault != num)
			{
				state.FromFraction = ((block.Items.Count == 0) ? 0.0 : ((double)valueOrDefault / (double)block.Items.Count));
				state.ChangedAt = InkObjectState.Now;
				state.ShownDone = num;
				state.Animate(InkSpring.SettleSeconds(0.4, 0.8));
			}
		}
		int valueOrDefault2 = state.ShownDone.GetValueOrDefault();
		if (!state.ShownDone.HasValue)
		{
			valueOrDefault2 = num;
			state.ShownDone = valueOrDefault2;
		}
		double fraction = InkSpring.Between(state.FromFraction, num2, 0.4, 0.8, state.ChangedAt);
		if (state.ChangedAt == -1000.0)
		{
			fraction = num2;
		}
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkHStack.Flexible(InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), Ink)), new ClickyInkSpacer(), InkViews.Plain($"{num} of {block.Items.Count}", ClickyPaperFonts.Hand(16.0), Ink.Opacity(0.6)))
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		ProgressStrip progressStrip = new ProgressStrip(fraction)
		{
			Height = 7.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		StackPanel stackPanel = InkViews.VStack(0.0, clickyInkHStack, progressStrip, new Border
		{
			Height = 1.0,
			Background = Ui.Brush(RuleBlue)
		});
		for (int j = 0; j < block.Items.Count; j++)
		{
			stackPanel.Children.Add(Row(block, block.Items[j], j, clock, style, actions, state));
			stackPanel.Children.Add(new Border
			{
				Height = 1.0,
				Background = Ui.Brush(RuleBlue)
			});
		}
		stackPanel.Margin = new Thickness(44.0, 12.0, 16.0, 14.0);
		StackPanel background = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 3.0,
			Margin = new Thickness(32.0, 0.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)new Border
				{
					Width = 1.0,
					Background = Ui.Brush(MarginRed)
				},
				(UIElement)new Border
				{
					Width = 1.0,
					Background = Ui.Brush(MarginRed)
				}
			}
		};
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel).Behind(background);
		InkPaperSheet inkPaperSheet = new InkPaperSheet("todo-glue", ClickyShape.UnevenRoundedRectangle(3.0, 0.0, 0.0, 3.0), PaperFill.Vertical((0.0, PaperColor.Hex("#C46A62")), (1.0, PaperColor.Hex("#B05A53"))), 0.3)
		{
			Height = 8.0
		};
		PaperDrawing paperDrawing = new PaperDrawing().Stroke(new PaperPath().MoveTo(0.0, 0.5).LineTo(2000.0, 0.5), new PaperStroke(PaperColor.Black.Opacity(0.1), 1.0, PenLineCap.Flat, PenLineJoin.Miter, new double[2] { 2.0, 3.0 }));
		paperDrawing.Height = 1.0;
		paperDrawing.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, -1.0, contentWidth - 8.0, 3.0)
		};
		StackPanel content = InkViews.VStack(0.0, inkPaperSheet, paperDrawing, clickyInkOverlay);
		InkPaperSheet background2 = new InkPaperSheet("todo-pad", ClickyShape.RoundedRectangle(3.0), PaperFill.Of(PaperColor.Hex("#FFFCEC")), 0.18, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.07), 2.0, 0.0, 1.0)
		});
		ClickyInkOverlay clickyInkOverlay2 = new ClickyInkOverlay(content).Behind(background2);
		clickyInkOverlay2.Width = contentWidth - 8.0;
		clickyInkOverlay2.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay2.Transformed(0.0, 0.0, -0.2, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkOverlay2 }
		};
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, int index, double clock, ClickyInkStyle style, ClickyInkActions actions, State state)
	{
		bool isChecked = item.Source.IsChecked == true;
		if (state.ShownTicks.TryGetValue(item.Id, out var value) && value != isChecked)
		{
			state.TickChangedAt[item.Id] = InkObjectState.Now;
			state.Animate(InkSpring.SettleSeconds(0.3, 0.7));
		}
		state.ShownTicks[item.Id] = isChecked;
		double num = (isChecked ? 1 : 0);
		if (state.TickChangedAt.TryGetValue(item.Id, out var value2))
		{
			num = InkSpring.Between((!isChecked) ? 1 : 0, isChecked ? 1 : 0, 0.3, 0.7, value2);
		}
		bool num2 = isChecked || num > 0.01;
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkBox(index + 3), new PaperStroke(Ink.Opacity(0.55), 1.3));
		clickyShapeView.Width = 15.0;
		clickyShapeView.Height = 15.0;
		clickyShapeView.HorizontalAlignment = HorizontalAlignment.Center;
		clickyShapeView.VerticalAlignment = VerticalAlignment.Center;
		Grid grid = new Grid
		{
			Width = 16.0,
			Height = 16.0,
			Children = { (UIElement)clickyShapeView }
		};
		if (num2)
		{
			ClickyShapeView clickyShapeView2 = ClickyShapeView.Stroke(new ClickyInkTick(), new PaperStroke(ClickyPaperInk.DoneGreen, 2.2, PenLineCap.Round, PenLineJoin.Round));
			clickyShapeView2.Width = 15.0;
			clickyShapeView2.Height = 14.0;
			clickyShapeView2.HorizontalAlignment = HorizontalAlignment.Center;
			clickyShapeView2.VerticalAlignment = VerticalAlignment.Center;
			clickyShapeView2.Transformed(2.0, -2.0, 0.0, 1.0, null);
			if (num < 0.999)
			{
				double num3 = 0.4 + 0.6 * num;
				Grid grid2 = new Grid
				{
					Width = 15.0,
					Height = 14.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				clickyShapeView2.HorizontalAlignment = HorizontalAlignment.Stretch;
				clickyShapeView2.VerticalAlignment = VerticalAlignment.Stretch;
				grid2.Children.Add(clickyShapeView2);
				grid2.Opacity = Math.Clamp(num, 0.0, 1.0);
				grid2.RenderTransformOrigin = new Point(0f, 1f);
				grid2.RenderTransform = new ScaleTransform
				{
					ScaleX = num3,
					ScaleY = num3
				};
				grid.Children.Add(grid2);
			}
			else
			{
				grid.Children.Add(clickyShapeView2);
			}
		}
		InkTextView inkTextView = InkViews.Words(item.Words, clock, style, ClickyPaperFonts.Hand(17.0), isChecked ? Ink.Opacity(0.45) : Ink, pencil: true, delegate(InkTextView t)
		{
			if (isChecked)
			{
				t.Strikethrough = Ink.Opacity(0.45);
			}
		});
		StackPanel stackPanel = InkViews.VStack(1.0, inkTextView);
		ClickyInkLiveNote.Words detail = item.Detail;
		if ((object)detail != null && ClickyInkWords.IsStarted(detail, clock))
		{
			stackPanel.Children.Add(InkViews.Words(detail, clock, style, ClickyPaperFonts.Typed(11.0, null), Ink.Opacity(0.55), pencil: false));
		}
		Pressable pressable = InkViews.PlainButton(new ClickyInkHStack(10.0, InkVerticalAlignment.FirstBaseline, grid.BaselineFromBottom(2.0), ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer())
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 6.0)
		}, delegate
		{
			actions.Toggle(block.Id, item.Id);
		}, ClickyInkNoteActions.Plain(item.Source.Text));
		pressable.HorizontalAlignment = HorizontalAlignment.Stretch;
		AutomationProperties.SetItemStatus(pressable, isChecked ? "done" : "not done");
		return pressable;
	}
}
