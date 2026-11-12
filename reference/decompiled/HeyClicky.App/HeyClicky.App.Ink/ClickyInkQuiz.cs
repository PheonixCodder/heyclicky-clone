using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkQuiz
{
	private sealed class State : InkObjectState
	{
		public readonly HashSet<string> Revealed = new HashSet<string>();

		public readonly Dictionary<string, double> TurnedAt = new Dictionary<string, double>();

		public readonly Dictionary<string, bool> Grades = new Dictionary<string, bool>();

		public readonly Dictionary<string, double> GradedAt = new Dictionary<string, double>();

		public DateTimeOffset? SentAt;

		public int Right => Grades.Values.Count((bool g) => g);
	}

	private static readonly Color Ink = PaperColor.Hex("#2B2620");

	private static readonly Color Red = PaperColor.Hex("#D9453B");

	private static readonly Color MissedRed = PaperColor.Hex("#C2410C");

	public static void SetForGallery(ClickyInkLiveNote.Block block, IEnumerable<string> revealedItemIDs, IReadOnlyDictionary<string, bool> grades)
	{
		State state = InkObjectState.For<State>(block, "quiz");
		foreach (string revealedItemID in revealedItemIDs)
		{
			state.Revealed.Add(revealedItemID);
			state.TurnedAt[revealedItemID] = -1000.0;
		}
		foreach (var (key, value) in grades)
		{
			state.Grades[key] = value;
			state.GradedAt[key] = -1000.0;
		}
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "quiz");
		return new InkObjectHost(state, () => Build(block, clock, style, actions, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, State state)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(9.0, InkVerticalAlignment.Center, ClickyInkRubberStamp.Make("flashcards", PaperColor.Hex("#2D5BE3"), -3.0), ClickyInkHStack.Flexible(InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), null)), new ClickyInkSpacer());
		if (state.Grades.Count > 0)
		{
			clickyInkHStack.Children.Add(InkViews.Plain($"{state.Right}/{state.Grades.Count} right", ClickyPaperFonts.Hand(16.0), style.Fresh));
		}
		StackPanel stackPanel = InkViews.VStack(14.0, clickyInkHStack);
		for (int j = 0; j < block.Items.Count; j++)
		{
			stackPanel.Children.Add(Card(block.Items[j], j + 1, j, clock, style, state, actions));
		}
		if (state.Grades.Count > 0)
		{
			DateTimeOffset? sentAt = state.SentAt;
			bool flag = sentAt.HasValue;
			ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(flag ? "Sent" : "Send my score", delegate
			{
				DateTimeOffset? sentAt2 = state.SentAt;
				if (!sentAt2.HasValue)
				{
					bool value;
					List<string> missed = (from i in block.Items
						where state.Grades.TryGetValue(i.Id, out value) && !value
						select i.Source.Text).ToList();
					actions.Perform(ClickyInkNoteActions.QuizResults(block.Source, state.Right, state.Grades.Count, missed).ForAnalytics("quiz_score_sent", null, new Dictionary<string, int>
					{
						["right"] = state.Right,
						["total"] = state.Grades.Count,
						["cards"] = block.Items.Count
					}));
					state.SentAt = DateTimeOffset.Now;
					state.Animate(InkSpring.SettleSeconds(0.3, 0.8));
				}
			}, flag ? ClickyPaperGel.Mint : ClickyPaperGel.Blue, 28.0, 13.0, 12.5, flag ? "checkmark" : null);
			if (flag)
			{
				clickyPaperGelButton.IsHitTestVisible = false;
			}
			Pressable pressable = InkViews.PlainButton(InkViews.Plain("Shuffle back", ClickyPaperFonts.Hand(15.0), style.Quiet), delegate
			{
				actions.Track("quiz_reset", ClickyInkDocument.Kind.Quiz, new JsonObject((JsonNodeOptions?)null)
				{
					["graded"] = state.Grades.Count,
					["cards"] = block.Items.Count
				});
				foreach (string item in state.Revealed)
				{
					state.TurnedAt[item] = InkObjectState.Now;
				}
				state.Revealed.Clear();
				state.Grades.Clear();
				state.GradedAt.Clear();
				state.SentAt = null;
				state.Animate(InkSpring.SettleSeconds(0.4, 0.85));
			});
			stackPanel.Children.Add(new ClickyInkHStack(10.0, InkVerticalAlignment.Center, clickyPaperGelButton, pressable));
		}
		stackPanel.Width = contentWidth;
		stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
		return stackPanel;
	}

	private static FrameworkElement Card(ClickyInkLiveNote.Item item, int number, int index, double clock, ClickyInkStyle style, State state, ClickyInkActions actions)
	{
		bool flag = state.Revealed.Contains(item.Id);
		bool value;
		bool? flag2 = (state.Grades.TryGetValue(item.Id, out value) ? new bool?(value) : ((bool?)null));
		string detail = item.Source.Detail;
		string obj = ((detail != null) ? ClickyInkNoteActions.Plain(detail) : "");
		bool flag3 = obj.Length > 0 && clock >= item.Words.FinishedAt;
		double num = (flag ? 1 : 0);
		if (state.TurnedAt.TryGetValue(item.Id, out var value2))
		{
			double num2 = InkSpring.Value(0.45, 0.8, InkObjectState.Now - value2);
			num = (flag ? num2 : (1.0 - num2));
		}
		FrameworkElement frameworkElement = Front(item, number, clock, style);
		FrameworkElement frameworkElement2 = Back(obj, number);
		frameworkElement.Opacity = Math.Clamp(1.0 - num, 0.0, 1.0);
		frameworkElement2.Opacity = Math.Clamp(num, 0.0, 1.0);
		Grid grid = new Grid
		{
			Children = 
			{
				(UIElement)frameworkElement,
				(UIElement)frameworkElement2
			}
		};
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
		if (num > 0.001 && num < 0.999)
		{
			grid.Projection = new PlaneProjection
			{
				RotationY = -180.0 * num
			};
			frameworkElement2.Projection = new PlaneProjection
			{
				RotationY = 180.0
			};
		}
		IClickyShape shape = ClickyShape.RoundedRectangle(4.0);
		PaperFill fill = PaperFill.Of(PaperColor.Hex("#FBF8F0"));
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.05), 1.0, 0.0, 0.5)
		};
		InkPaperSheet inkPaperSheet = new InkPaperSheet("quiz-under", shape, fill, null, shadows);
		inkPaperSheet.Transformed(2.0, 2.0, (index % 2 == 0) ? 0.9 : (-0.8), 1.0, null);
		List<UIElement> list = new List<UIElement>();
		bool hasValue = flag2.HasValue;
		double valueOrDefault = state.GradedAt.GetValueOrDefault(item.Id, -1000.0);
		if (hasValue)
		{
			double num3 = Math.Clamp(InkSpring.Value(0.3, 0.7, InkObjectState.Now - valueOrDefault), 0.0, 1.2);
			FrameworkElement item2 = ClickyInkRubberStamp.Make((flag2 == true) ? "got it" : "missed", (flag2 == true) ? ClickyPaperInk.DoneGreen : MissedRed, (flag2 == true) ? 7 : (-6));
			Grid grid2 = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 9.0, 10.0, 0.0),
				Opacity = Math.Clamp(num3, 0.0, 1.0),
				Children = { (UIElement)item2 }
			};
			grid2.Transformed(0.0, 0.0, 0.0, 1.7 - 0.7 * num3, null);
			list.Add(grid2);
		}
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(grid, list.ToArray()).Behind(inkPaperSheet);
		FrameworkElement frameworkElement3 = clickyInkOverlay;
		if (flag3)
		{
			Pressable pressable = InkViews.PlainButton(clickyInkOverlay, delegate
			{
				if (!state.Revealed.Contains(item.Id))
				{
					actions.Track("quiz_card_revealed", ClickyInkDocument.Kind.Quiz, new JsonObject((JsonNodeOptions?)null) { ["card_index"] = index });
				}
				if (!state.Revealed.Remove(item.Id))
				{
					state.Revealed.Add(item.Id);
				}
				state.TurnedAt[item.Id] = InkObjectState.Now;
				state.Animate(InkSpring.SettleSeconds(0.45, 0.8));
			}, flag ? "Shows the question" : "Shows the answer");
			pressable.HorizontalAlignment = HorizontalAlignment.Stretch;
			frameworkElement3 = pressable;
		}
		StackPanel stackPanel = InkViews.VStack(8.0, frameworkElement3);
		if (flag3)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0,
				Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
			};
			if (!flag)
			{
				stackPanel2.Children.Add(InkViews.Plain("tap to turn it over", ClickyPaperFonts.Hand(14.0), style.Quiet));
			}
			else if (!flag2.HasValue)
			{
				stackPanel2.Children.Add(new ClickyPaperGelButton("Got it", delegate
				{
					Grade(right: true);
				}, ClickyPaperGel.Mint, 24.0, 10.0, 11.5));
				stackPanel2.Children.Add(new ClickyPaperGelButton("Missed it", delegate
				{
					Grade(right: false);
				}, ClickyPaperGel.White, 24.0, 10.0, 11.5));
			}
			stackPanel.Children.Add(stackPanel2);
		}
		stackPanel.Transformed(0.0, 0.0, (index % 2 == 0) ? (-0.3) : 0.25, 1.0, null);
		return stackPanel;
		void Grade(bool right)
		{
			actions.Track("quiz_card_graded", ClickyInkDocument.Kind.Quiz, new JsonObject((JsonNodeOptions?)null)
			{
				["result"] = (right ? "got_it" : "missed"),
				["card_index"] = index
			});
			state.Grades[item.Id] = right;
			state.GradedAt[item.Id] = InkObjectState.Now;
			state.Animate(InkSpring.SettleSeconds(0.3, 0.7));
		}
	}

	private static FrameworkElement Front(ClickyInkLiveNote.Item item, int number, double clock, ClickyInkStyle style)
	{
		StackPanel stackPanel = InkViews.VStack(8.0, InkViews.Plain($"Q{number}", InkObjectFonts.Rounded(12.0, FontWeights.ExtraBold), Red), InkViews.Words(item.Words, clock, style, ClickyPaperFonts.Typed(14.5, FontWeights.Medium), Ink));
		stackPanel.Margin = new Thickness(15.0, 12.0, 15.0, 12.0);
		InkPaperSheet item2 = new InkPaperSheet("quiz-front", ClickyShape.RoundedRectangle(4.0), PaperFill.Of(PaperColor.Hex("#FFFEF9")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.13), 2.5, 0.0, 1.5)
		});
		Border item3 = new Border
		{
			Height = 1.0,
			Background = Ui.Brush(Red.Opacity(0.4)),
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 30.0, 0.0, 0.0),
			IsHitTestVisible = false
		};
		return new Grid
		{
			MinHeight = 74.0,
			Children = 
			{
				(UIElement)item2,
				(UIElement)item3,
				(UIElement)stackPanel
			}
		};
	}

	private static FrameworkElement Back(string answer, int number)
	{
		StackPanel stackPanel = InkViews.VStack(6.0, InkViews.Plain($"A{number}", InkObjectFonts.Rounded(12.0, FontWeights.ExtraBold), ClickyPaperInk.DoneGreen), InkViews.Plain(answer, ClickyPaperFonts.Hand(17.0), Ink));
		stackPanel.Margin = new Thickness(15.0, 12.0, 15.0, 12.0);
		InkPaperSheet item = new InkPaperSheet("quiz-back", ClickyShape.RoundedRectangle(4.0), PaperFill.Of(PaperColor.Hex("#FFF7D6")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.13), 2.5, 0.0, 1.5)
		});
		return new Grid
		{
			MinHeight = 74.0,
			Children = 
			{
				(UIElement)item,
				(UIElement)stackPanel
			}
		};
	}
}
